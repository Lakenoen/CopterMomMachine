#include "ExecController.h"
#include <algorithm>
#include <shlwapi.h>

std::string toLower(const std::string& str) {
    std::string lower = str;
    std::transform(lower.begin(), lower.end(), lower.begin(),
        [](unsigned char c) { return std::tolower(c); });
    return lower;
}

ExecController::ExecController(SimpleArray& data)
{
    init(data);
}

ExecController::~ExecController()
{
    VirtualFree((void*)peData.imageBase, 0, MEM_RELEASE);
}

void ExecController::init(SimpleArray& data)
{
    PEInfo payload{ 0 };
    DWORD oldProtect;
    VirtualProtect(data.data, data.size, PAGE_READWRITE, &oldProtect);

    payload.dosHeader = reinterpret_cast<IMAGE_DOS_HEADER*>(data.data);

    if (payload.dosHeader->e_magic != IMAGE_DOS_SIGNATURE) {
        throw std::exception("Invalid DOS header");
    }

#if defined(_WIN64)
    payload.ntHeaders = reinterpret_cast<IMAGE_NT_HEADERS64*>(data.data + payload.dosHeader->e_lfanew);
#else
    payload.ntHeaders = reinterpret_cast<IMAGE_NT_HEADERS32*>(data.data + payload.dosHeader->e_lfanew);
#endif

    if (payload.ntHeaders->Signature != IMAGE_NT_SIGNATURE) {
        throw std::exception("Invalid NT header");
    }

    payload.imageSize = (payload.ntHeaders->OptionalHeader.SizeOfImage + payload.ntHeaders->OptionalHeader.SectionAlignment - 1)
        & ~(payload.ntHeaders->OptionalHeader.SectionAlignment - 1);
    payload.imageBase = (std::byte*)(VirtualAlloc(NULL, payload.imageSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE));
    if (!payload.imageBase)
        throw std::exception("Virtual mem alloc error");

    memcpy_s(payload.imageBase, payload.ntHeaders->OptionalHeader.SizeOfHeaders, data.data, payload.ntHeaders->OptionalHeader.SizeOfHeaders);

#if defined(_WIN64)
    payload.ntHeaders = reinterpret_cast<IMAGE_NT_HEADERS64*>(payload.imageBase + payload.dosHeader->e_lfanew);
#else
    payload.ntHeaders = reinterpret_cast<IMAGE_NT_HEADERS*>(payload.imageBase + payload.dosHeader->e_lfanew);
#endif

    payload.dosHeader = (IMAGE_DOS_HEADER*)payload.imageBase;

    IMAGE_SECTION_HEADER* sectionHeader = IMAGE_FIRST_SECTION(payload.ntHeaders);
    for (int i = 0; i < payload.ntHeaders->FileHeader.NumberOfSections; ++i) {
        memcpy_s(
            payload.imageBase + sectionHeader[i].VirtualAddress,
            sectionHeader[i].SizeOfRawData,
            data.data + sectionHeader[i].PointerToRawData,
            sectionHeader[i].SizeOfRawData
        );
    }

    applyRelocation(payload);
    fixImports(payload);
    fixTLS(payload);
    setSectionProtect(payload);
    this->peData = payload;
    this->entryPoint = (EntryPointFunc)(peData.imageBase + peData.ntHeaders->OptionalHeader.AddressOfEntryPoint);
}

void ExecController::fixTLS(PEInfo& target)
{
#if defined(_WIN64)
    PIMAGE_TLS_DIRECTORY64 dir = (PIMAGE_TLS_DIRECTORY64)(target.imageBase + target.ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS].VirtualAddress);
#else
    PIMAGE_TLS_DIRECTORY dir = (PIMAGE_TLS_DIRECTORY)(target.imageBase + target.ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS].VirtualAddress);
#endif

    if (!dir || !target.ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS].Size) {
        return;
    }

    if (dir->AddressOfCallBacks) {
        auto callback = (PIMAGE_TLS_CALLBACK*)dir->AddressOfCallBacks;
        while (*callback) {
            (*callback)(target.imageBase, DLL_PROCESS_ATTACH, nullptr);
            callback++;
        }
    }

    if (dir->StartAddressOfRawData && dir->EndAddressOfRawData) {
        size_t tlsSize = dir->EndAddressOfRawData - dir->StartAddressOfRawData;
        auto tlsData = VirtualAlloc(NULL, tlsSize, MEM_COMMIT, PAGE_READWRITE);
        memcpy_s(tlsData, tlsSize, (void*)dir->StartAddressOfRawData, tlsSize);
    }

}

void ExecController::setSectionProtect(PEInfo& payload) {
    IMAGE_SECTION_HEADER* sectionHeader = IMAGE_FIRST_SECTION(payload.ntHeaders);
    for (int i = 0; i < payload.ntHeaders->FileHeader.NumberOfSections; ++i) {

        void* sectionAbsAddr = payload.imageBase + sectionHeader[i].VirtualAddress;

        PTR alignSectionSize = (sectionHeader[i].Misc.VirtualSize + payload.ntHeaders->OptionalHeader.SectionAlignment - 1)
            & ~(payload.ntHeaders->OptionalHeader.SectionAlignment - 1);

        DWORD protect = 0;

        if (sectionHeader[i].Characteristics & IMAGE_SCN_MEM_EXECUTE) {
            if (sectionHeader[i].Characteristics & IMAGE_SCN_MEM_WRITE) {
                protect = PAGE_EXECUTE_READWRITE;
            }
            else if (sectionHeader[i].Characteristics & IMAGE_SCN_MEM_READ) {
                protect = PAGE_EXECUTE_READ;
            }
            else {
                protect = PAGE_EXECUTE;
            }
        }
        else if (sectionHeader[i].Characteristics & IMAGE_SCN_MEM_READ) {
            if (sectionHeader[i].Characteristics & IMAGE_SCN_MEM_WRITE) {
                protect = PAGE_READWRITE;
            }
            else {
                protect = PAGE_READONLY;
            }
        }
        else if (sectionHeader[i].Characteristics & IMAGE_SCN_MEM_WRITE) {
            protect = PAGE_READWRITE;
        }
        else {
            protect = PAGE_NOACCESS;
        }

        DWORD oldProtect = 0;
        VirtualProtect(sectionAbsAddr, alignSectionSize, protect, &oldProtect);
    }

}

void ExecController::setHook(void* fakeFuncAddr, void* originalFuncAddr)
{
    if (originalFuncAddr == nullptr)
        throw std::invalid_argument("Hook error: original address has been is null");
    if (hooked.count((unsigned int)fakeFuncAddr))
        return;
    auto payload = std::make_pair(std::array<std::byte, 8>(),originalFuncAddr);
    hooked.insert(std::make_pair((PTR)fakeFuncAddr, payload));
    DWORD old_protect;
    VirtualProtect(originalFuncAddr, 5, PAGE_EXECUTE_READWRITE, &old_protect);
    std::byte stored[8];
    memcpy(hooked[(PTR)fakeFuncAddr].first.data(), originalFuncAddr, 5);
    BYTE jmp[5] = { 0xE9, 0x00, 0x00, 0x00, 0x00 };
    DWORD offset = (DWORD)fakeFuncAddr - (DWORD)originalFuncAddr - 5;
    memcpy(&jmp[1], &offset, 4);
    memcpy(originalFuncAddr, jmp, 5);
    VirtualProtect(originalFuncAddr, 5, old_protect, &old_protect);
}

void ExecController::removeHook(void* fakeFuncAddr)
{
    auto it = hooked.find((PTR)fakeFuncAddr);
    if (it == hooked.end())
        return;
    DWORD old_protect;
    VirtualProtect(it->second.second, 5, PAGE_EXECUTE_READWRITE, &old_protect);
    memcpy(it->second.second, (void*)it->second.first.data(), 5);
    VirtualProtect(it->second.second, 5, old_protect, &old_protect);
    hooked.erase((PTR)fakeFuncAddr);
}

void ExecController::applyRelocation(PEInfo& payload) {
    PTR delta = (PTR)payload.imageBase - payload.ntHeaders->OptionalHeader.ImageBase;

    if (!delta) return;

    payload.relocTable = &payload.ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC];

    if (payload.relocTable->Size == 0)
        return;

    IMAGE_BASE_RELOCATION* relocBlock = (IMAGE_BASE_RELOCATION*)(payload.imageBase + payload.relocTable->VirtualAddress);

    while (relocBlock->VirtualAddress) {
        std::byte* relocData = (std::byte*)(relocBlock) + sizeof(IMAGE_BASE_RELOCATION);
        int num = (relocBlock->SizeOfBlock - sizeof(IMAGE_BASE_RELOCATION)) / sizeof(unsigned short);
        for (unsigned int i = 0; i < num; ++i) {
            unsigned short entry = ((unsigned short*)relocData)[i];
            int type = entry >> 12;
            int offset = entry & 0xFFF;
#if defined(_WIN64)
            if (type == IMAGE_REL_BASED_DIR64) {
#else
            if (type == IMAGE_REL_BASED_HIGHLOW) {
#endif
                PTR* address = (PTR*)(payload.imageBase + relocBlock->VirtualAddress + offset);
                *address += delta;
            }
        }
        relocBlock = (IMAGE_BASE_RELOCATION*)((std::byte*)relocBlock + relocBlock->SizeOfBlock);
    }
}

void ExecController::fixImports(PEInfo& payload) {
    payload.importTable = &payload.ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];

    if (payload.importTable->Size == 0) return;

    IMAGE_IMPORT_DESCRIPTOR* importDesc = (IMAGE_IMPORT_DESCRIPTOR*)(payload.imageBase + payload.importTable->VirtualAddress);

    if (!importDesc)
        return;

    while (importDesc->Name) {

        const char* dllName = reinterpret_cast<const char*>(payload.imageBase + importDesc->Name);

        HMODULE hModule = LoadLibraryA(dllName);
        if (!hModule) {
            std::string errorMsg = "Failed to load DLL " + std::string(dllName);
            throw std::exception(errorMsg.c_str());
        }
#if defined(_WIN64)
        IMAGE_THUNK_DATA64* thunk = (IMAGE_THUNK_DATA64*)(payload.imageBase + importDesc->OriginalFirstThunk);
        IMAGE_THUNK_DATA64* iat = (IMAGE_THUNK_DATA64*)(payload.imageBase + importDesc->FirstThunk);
#else
        IMAGE_THUNK_DATA* thunk = (IMAGE_THUNK_DATA*)(payload.imageBase + importDesc->OriginalFirstThunk);
        IMAGE_THUNK_DATA* iat = (IMAGE_THUNK_DATA*)(payload.imageBase + importDesc->FirstThunk);
#endif

        if (!thunk || !iat) {
            std::string errorMsg = "Failed to load DLL " + std::string(dllName);
            throw std::exception(errorMsg.c_str());
        }
        
        while (thunk->u1.AddressOfData) {
#if defined(_WIN64)
            if (thunk->u1.Ordinal & IMAGE_ORDINAL_FLAG64) {
#else
            if (thunk->u1.Ordinal & IMAGE_ORDINAL_FLAG) {
#endif
                FARPROC func = GetProcAddress(hModule, (LPCSTR)(thunk->u1.Ordinal & 0xFFFF));
                if (!func) {
                    throw std::exception("Failure find a function on ordinal");
                }
                iat->u1.Function = reinterpret_cast<PTR>(func);
            }
            else {
                IMAGE_IMPORT_BY_NAME* importByName = reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(payload.imageBase + thunk->u1.AddressOfData);
                FARPROC func = GetProcAddress(hModule, importByName->Name);
                if (!func) {
                    std::string errorMsg = "Failure find a function: " + std::string(importByName->Name);
                    throw std::exception(errorMsg.c_str());
                }
                iat->u1.Function = reinterpret_cast<PTR>(func);
            }

            thunk++;
            iat++;
        }

        importDesc++;
    }
}

void FakeExitProcess(UINT code) {
    TerminateThread(GetCurrentThread(), 0);
}

BOOL FakeTerminateProcess(HANDLE proc,UINT code) {
    TerminateThread(GetCurrentThread(), 0);
    return true;
}

void ExecController::run()
{
    this->setRedirect("exit",(void*)FakeExitProcess);
    this->setRedirect("TerminateProcess", (void*)FakeTerminateProcess);
    this->setRedirect("ExitProcess", (void*)FakeExitProcess);

    HANDLE thread = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)this->entryPoint, NULL, 0, NULL);

    if (thread == INVALID_HANDLE_VALUE || thread == NULL) 
        throw std::exception("It was not possible to start entry point");

    WaitForSingleObject(thread,INFINITE);

   while (!hooked.empty())
        removeHook((void*)hooked.begin()->first);

   while (!redirectedFunctions.empty())
       removeRedirect(redirectedFunctions.begin()->first);

}

void ExecController::setRedirect(std::string funñName, void* newFunction)
{
    IMAGE_IMPORT_DESCRIPTOR* importDesc = (IMAGE_IMPORT_DESCRIPTOR*)(this->peData.imageBase + this->peData.importTable->VirtualAddress);

    while (importDesc->Name) {
#if defined(_WIN64)
        PIMAGE_THUNK_DATA64 iat = (PIMAGE_THUNK_DATA64)(this->peData.imageBase + importDesc->FirstThunk);
        PIMAGE_THUNK_DATA64 thunk = (PIMAGE_THUNK_DATA64)(this->peData.imageBase + importDesc->OriginalFirstThunk);
#else
        PIMAGE_THUNK_DATA iat = (PIMAGE_THUNK_DATA)(this->peData.imageBase + importDesc->FirstThunk);
        PIMAGE_THUNK_DATA thunk = (PIMAGE_THUNK_DATA)(this->peData.imageBase + importDesc->OriginalFirstThunk);
#endif
        if (!thunk)
            thunk = iat;

        while (thunk->u1.AddressOfData) {
#if defined(_WIN64)
            if (thunk->u1.Ordinal & IMAGE_ORDINAL_FLAG64) {
#else
            if (thunk->u1.Ordinal & IMAGE_ORDINAL_FLAG) {
#endif
                ++iat;
                ++thunk;
                continue;
            }
            PIMAGE_IMPORT_BY_NAME import = (PIMAGE_IMPORT_BY_NAME)(peData.imageBase + thunk->u1.AddressOfData);
            std::string currentFuncName = import->Name;
            if (toLower(currentFuncName) == toLower(funñName)) {
                DWORD old = 0;
                VirtualProtect(&iat->u1.Function, sizeof(PTR), PAGE_READWRITE, &old);
                redirectedFunctions.insert(std::make_pair(currentFuncName, iat->u1.Function));
                iat->u1.Function = (PTR)newFunction;

                VirtualProtect(&iat->u1.Function, sizeof(PTR), old, &old);
                return;
            }
            ++iat;
            ++thunk;
        }
        ++importDesc;
    }

}

void ExecController::removeRedirect(std::string funñName)
{
    if (redirectedFunctions.count(funñName)) {
        setRedirect(funñName, (void*)redirectedFunctions.at(funñName));
        redirectedFunctions.erase(funñName);
    }
}

EntryPointFunc ExecController::getEntryPoint()
{
    return this->entryPoint;
}
