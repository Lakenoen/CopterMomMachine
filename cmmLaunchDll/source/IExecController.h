#ifndef IEXEC_H
#define IEXEC_H
#include "PEInfo.h"
#include <string>
#include <vector>
#include <memory>
using EntryPointFunc = void(*)();
class IExecController
{
public:
    virtual void init(SimpleArray& data) = 0;
    virtual void run() = 0;
    virtual void setRedirect(std::string funñName,void* newFunction) = 0;
    virtual void removeRedirect(std::string funñName) = 0;
    virtual EntryPointFunc getEntryPoint() = 0;
};
#endif