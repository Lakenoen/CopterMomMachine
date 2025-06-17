#ifndef EXEC_H
#define EXEC_H

#include "IExecController.h"
#include <iostream>
#include <fstream>
#include <unordered_map>
#include <array>
#include <map>

#if defined(_WIN64)
using PTR = unsigned long long;
#else
using PTR = unsigned long;
#endif

class ExecController : public IExecController
{
private:
    std::unordered_map<PTR, std::pair<std::array<std::byte,8>,void*>> hooked;
    std::unordered_map<std::string, PTR> redirectedFunctions;
    PEInfo peData;
    EntryPointFunc entryPoint = nullptr;
public:
    ExecController(SimpleArray& data);
    ~ExecController();
    virtual void init(SimpleArray& data);
    virtual void run();
    virtual void setRedirect(std::string funñName, void* newFunction);
    virtual void removeRedirect(std::string funñName);
    virtual EntryPointFunc getEntryPoint();
private:
    void applyRelocation(PEInfo& target);
    void fixImports(PEInfo& target);
    void fixTLS(PEInfo& target);
    void setSectionProtect(PEInfo& target);
    void setHook(void* fakeFuncAddr,void* originalFuncAddr);
    void removeHook(void* fakeFuncAddr);
};

#endif