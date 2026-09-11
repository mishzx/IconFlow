#include <windows.h>
#include <shobjidl.h>
#include <shellapi.h>
#include <shlwapi.h>
#include <atomic>
#include <new>
#include <string>

#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "ole32.lib")

// {7F49E25B-47E3-47F7-B49A-2CD3F4C6A301}
static const CLSID CLSID_IconFlowCommand =
{ 0x7f49e25b, 0x47e3, 0x47f7, { 0xb4, 0x9a, 0x2c, 0xd3, 0xf4, 0xc6, 0xa3, 0x01 } };

static HMODULE g_module = nullptr;
static std::atomic<long> g_objects{ 0 };

static HRESULT DuplicateString(const wchar_t* value, wchar_t** result)
{
    if (!result) return E_POINTER;
    *result = nullptr;
    const auto bytes = (wcslen(value) + 1) * sizeof(wchar_t);
    auto copy = static_cast<wchar_t*>(CoTaskMemAlloc(bytes));
    if (!copy) return E_OUTOFMEMORY;
    memcpy(copy, value, bytes);
    *result = copy;
    return S_OK;
}

static std::wstring ApplicationPath()
{
    wchar_t modulePath[32768]{};
    GetModuleFileNameW(g_module, modulePath, static_cast<DWORD>(std::size(modulePath)));
    PathRemoveFileSpecW(modulePath);
    std::wstring result(modulePath);
    result += L"\\IconFlow.exe";
    return result;
}

class IconFlowCommand final : public IExplorerCommand
{
    std::atomic<ULONG> references_{ 1 };
public:
    IconFlowCommand() { ++g_objects; }
    ~IconFlowCommand() { --g_objects; }

    IFACEMETHODIMP QueryInterface(REFIID iid, void** object) override
    {
        if (!object) return E_POINTER;
        *object = nullptr;
        if (iid == IID_IUnknown || iid == __uuidof(IExplorerCommand)) *object = static_cast<IExplorerCommand*>(this);
        else return E_NOINTERFACE;
        AddRef();
        return S_OK;
    }
    IFACEMETHODIMP_(ULONG) AddRef() override { return ++references_; }
    IFACEMETHODIMP_(ULONG) Release() override
    {
        const auto value = --references_;
        if (!value) delete this;
        return value;
    }

    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* name) override { return DuplicateString(L"使用 IconFlow 更换图标", name); }
    IFACEMETHODIMP GetIcon(IShellItemArray*, LPWSTR* icon) override { return DuplicateString(ApplicationPath().c_str(), icon); }
    IFACEMETHODIMP GetToolTip(IShellItemArray*, LPWSTR* tooltip) override { return DuplicateString(L"打开轻量图标库", tooltip); }
    IFACEMETHODIMP GetCanonicalName(GUID* guid) override { if (!guid) return E_POINTER; *guid = CLSID_IconFlowCommand; return S_OK; }
    IFACEMETHODIMP GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) override
    {
        if (!state) return E_POINTER;
        *state = ECS_HIDDEN;
        if (!items) return S_OK;
        DWORD count = 0;
        if (FAILED(items->GetCount(&count)) || count != 1) return S_OK;
        IShellItem* item = nullptr;
        if (FAILED(items->GetItemAt(0, &item))) return S_OK;
        SFGAOF attributes = 0;
        const bool folder = SUCCEEDED(item->GetAttributes(SFGAO_FOLDER, &attributes)) && (attributes & SFGAO_FOLDER) != 0;
        PWSTR path = nullptr;
        const bool shortcut = SUCCEEDED(item->GetDisplayName(SIGDN_FILESYSPATH, &path))
            && path && _wcsicmp(PathFindExtensionW(path), L".lnk") == 0;
        if (path) CoTaskMemFree(path);
        item->Release();
        if (folder || shortcut) *state = ECS_ENABLED;
        return S_OK;
    }
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) override
    {
        if (!items) return E_INVALIDARG;
        IShellItem* item = nullptr;
        auto result = items->GetItemAt(0, &item);
        if (FAILED(result)) return result;
        PWSTR path = nullptr;
        result = item->GetDisplayName(SIGDN_FILESYSPATH, &path);
        item->Release();
        if (FAILED(result)) return result;
        const auto executable = ApplicationPath();
        std::wstring arguments = L"--change-icon \"";
        arguments += path;
        arguments += L"\"";
        CoTaskMemFree(path);
        const auto launch = reinterpret_cast<INT_PTR>(ShellExecuteW(nullptr, L"open", executable.c_str(), arguments.c_str(), nullptr, SW_SHOWNORMAL));
        return launch > 32 ? S_OK : HRESULT_FROM_WIN32(static_cast<DWORD>(launch));
    }
    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) override { if (!flags) return E_POINTER; *flags = ECF_DEFAULT; return S_OK; }
    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** commands) override { if (commands) *commands = nullptr; return E_NOTIMPL; }
};

class ClassFactory final : public IClassFactory
{
    std::atomic<ULONG> references_{ 1 };
public:
    IFACEMETHODIMP QueryInterface(REFIID iid, void** object) override
    {
        if (!object) return E_POINTER;
        *object = nullptr;
        if (iid == IID_IUnknown || iid == IID_IClassFactory) *object = static_cast<IClassFactory*>(this);
        else return E_NOINTERFACE;
        AddRef();
        return S_OK;
    }
    IFACEMETHODIMP_(ULONG) AddRef() override { return ++references_; }
    IFACEMETHODIMP_(ULONG) Release() override { const auto value = --references_; if (!value) delete this; return value; }
    IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID iid, void** object) override
    {
        if (outer) return CLASS_E_NOAGGREGATION;
        auto command = new (std::nothrow) IconFlowCommand();
        if (!command) return E_OUTOFMEMORY;
        const auto result = command->QueryInterface(iid, object);
        command->Release();
        return result;
    }
    IFACEMETHODIMP LockServer(BOOL lock) override { if (lock) ++g_objects; else --g_objects; return S_OK; }
};

extern "C" BOOL WINAPI DllMain(HMODULE module, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH) { g_module = module; DisableThreadLibraryCalls(module); }
    return TRUE;
}

STDAPI DllGetClassObject(REFCLSID clsid, REFIID iid, void** object)
{
    if (clsid != CLSID_IconFlowCommand) return CLASS_E_CLASSNOTAVAILABLE;
    auto factory = new (std::nothrow) ClassFactory();
    if (!factory) return E_OUTOFMEMORY;
    const auto result = factory->QueryInterface(iid, object);
    factory->Release();
    return result;
}

STDAPI DllCanUnloadNow() { return g_objects.load() == 0 ? S_OK : S_FALSE; }
