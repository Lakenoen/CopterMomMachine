#ifndef PEINFO_H
#define PEINFO_H
#include <windows.h>
#include <memory>
#include <vector>

class SimpleArray {
public:
    std::byte* data = nullptr;
    size_t size = 0;
    bool isClearDataAfterDestroy = false;
public:
    SimpleArray(std::byte* data,size_t size) : size(size), data(data)
    {
        
    }

    void setClearData(bool isClear) {
        this->isClearDataAfterDestroy = isClear;
    }

    ~SimpleArray() {
        if(isClearDataAfterDestroy)
            delete[] data;
    }

    SimpleArray() = delete;
};

struct PEInfo{
public:
    IMAGE_DOS_HEADER* dosHeader = nullptr;
#if defined(_WIN64)
    IMAGE_NT_HEADERS64* ntHeaders = nullptr;
#else
    IMAGE_NT_HEADERS32* ntHeaders = nullptr;
#endif
    IMAGE_DATA_DIRECTORY* relocTable = nullptr;
    IMAGE_DATA_DIRECTORY* importTable = nullptr;
    std::byte* imageBase = nullptr;
    unsigned int imageSize = 0;
};
#endif