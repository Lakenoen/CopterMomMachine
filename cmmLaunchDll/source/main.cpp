
#include "ExecController.h"
#include <map>
#include <memory>
#include <mutex>

static std::shared_ptr<std::map<unsigned int, std::shared_ptr<ExecController>>> pGlobalMap = nullptr;
static std::mutex gMapMutex;

extern "C" 
{
    EntryPointFunc __cdecl init(unsigned int id, std::byte* data, unsigned long size, const char* errorMsg = "") {
        EntryPointFunc res = nullptr;
        try {
            gMapMutex.lock();
            if (pGlobalMap == nullptr)
                pGlobalMap = std::make_shared<std::map<unsigned int, std::shared_ptr<ExecController>>>();
            if (pGlobalMap->count(id)) {
                gMapMutex.unlock();
                return nullptr;
            }

            gMapMutex.unlock();

            SimpleArray arr(data, size);
            arr.isClearDataAfterDestroy = false;
            std::shared_ptr<ExecController> newController = std::make_shared<ExecController>(arr);
            res = newController->getEntryPoint();
            std::lock_guard<std::mutex> guard(gMapMutex);

            pGlobalMap->insert(std::make_pair(id, newController));

        }
        catch (std::exception& ex){ 
            errorMsg = ex.what();
        }
        return res;
    }

    void __cdecl run(unsigned int id, const char* errorMsg = "") {
        try {
            gMapMutex.lock();
            if (pGlobalMap == nullptr || !pGlobalMap->count(id)) {
                gMapMutex.unlock();
                return;
            }
            std::shared_ptr<ExecController> controller = pGlobalMap->operator[](id);
            controller->run();
        }
        catch (std::exception& ex) {
            errorMsg = ex.what();
        }
        gMapMutex.unlock();
    }

    void __cdecl close(unsigned int id, const char* errorMsg = "") {
        try {
            if (pGlobalMap == nullptr || !pGlobalMap->count(id))
                return;
            std::lock_guard<std::mutex> guard(gMapMutex);
            if (pGlobalMap == nullptr || !pGlobalMap->count(id)) {
                return;
            }
            pGlobalMap->erase(id);
        }
        catch (std::exception& ex) {
            errorMsg = ex.what();
        }
    }

    void __cdecl setRedirect(unsigned int id, const char* funcName, void* func, const char* errorMsg = "") {
        try {
            gMapMutex.lock();
            if (pGlobalMap == nullptr || !pGlobalMap->count(id)) {
                gMapMutex.unlock();
                return;
            }
            (*pGlobalMap)[id]->setRedirect(funcName,func);
        }
        catch (std::exception& ex) {
            errorMsg = ex.what();
        }
        gMapMutex.unlock();
    }

    void __cdecl removeRedirect(unsigned int id, const char* funcName, const char* errorMsg) {
        try {
            gMapMutex.lock();
            if (pGlobalMap == nullptr || !pGlobalMap->count(id)) {
                gMapMutex.unlock();
                return;
            }
            (*pGlobalMap)[id]->removeRedirect(funcName);
        }
        catch (std::exception& ex) {
            errorMsg = ex.what();
        }
        gMapMutex.unlock();
    }

    void __cdecl test() {
#if defined(_WIN64)
        const char* exePath = "D:\\BakkesMod\\BakkesMod.exe";
#else
        const char* exePath = "D:\\code\\cpp\\TestAppCpp\\Release\\TestAppCpp.exe";
#endif

        HANDLE hFile = CreateFileA(exePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
        if (hFile == INVALID_HANDLE_VALUE) {
            std::cerr << "Не удалось открыть файл: " << exePath << std::endl;
            return;
        }

        DWORD fileSize = GetFileSize(hFile, NULL);
        if (fileSize == INVALID_FILE_SIZE) {
            std::cerr << "Не удалось получить размер файла: " << exePath << std::endl;
            CloseHandle(hFile);
            return;
        }

        SimpleArray arr(new std::byte[fileSize], fileSize);
        DWORD bytesRead;
        if (!ReadFile(hFile, arr.data, arr.size, &bytesRead, NULL)) {
            std::cerr << "Не удалось прочитать файл в память" << std::endl;
            CloseHandle(hFile);
            return;
        }
        CloseHandle(hFile);
        init(1, arr.data, arr.size);
        run(1);
        close(1);
    }

}

#ifdef DEBUG

int main(){
    test();
    ExitProcess(0);
}

#endif