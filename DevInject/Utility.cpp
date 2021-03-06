﻿#include "stdafx.h"
#include "Main.h"
#include "Utility.h"

static UINT WM_CUSTOM_DETACH = ::RegisterWindowMessage(L"DevInject::WM_CUSTOM_DETACH");
static UINT WM_CUSTOM_ATTACHED = ::RegisterWindowMessage(L"DevInject::WM_CUSTOM_ATTACHED");
static const bool IsMyProcess64 = (PLATFORM_BITS == 64);

UINT DevInject::GetDetachMessage()
{
    return ::WM_CUSTOM_DETACH;
}

UINT DevInject::GetAttachedMessage()
{
    return ::WM_CUSTOM_ATTACHED;
}

std::wstring DevInject::GetModuleFileName(HMODULE handle)
{
    wchar_t staticBuffer[MAX_PATH];
    std::vector<BYTE> dynamicBuffer;

    wchar_t* curBuffer = staticBuffer;
    DWORD curBufferSize = _countof(staticBuffer);

    while (true)
    {
        DWORD size = ::GetModuleFileName(handle, curBuffer, curBufferSize);
        if (size >= curBufferSize)
        {
            dynamicBuffer.resize(static_cast<size_t>(curBufferSize) + MAX_PATH);
            curBufferSize = static_cast<DWORD>(dynamicBuffer.size());
        }
        else
        {
            return std::wstring(curBuffer, size);
        }
    }
}

static bool InjectDllSameBitness(HANDLE process, HANDLE stopEvent)
{
    HMODULE remoteModule = nullptr;
    std::wstring dllPath = DevInject::GetModuleFileName(DevInject::GetModule());
    size_t dllPathSize = (dllPath.size() + 1) * sizeof(wchar_t);

    wchar_t* dllPathRemote = reinterpret_cast<wchar_t*>(::VirtualAllocEx(process, nullptr, dllPathSize, MEM_COMMIT, PAGE_READWRITE));
    if (dllPathRemote)
    {
        if (::WriteProcessMemory(process, dllPathRemote, dllPath.c_str(), dllPathSize, nullptr))
        {
            HMODULE kernelModule = ::GetModuleHandle(L"Kernel32");
            LPTHREAD_START_ROUTINE loadLibrary = kernelModule ? reinterpret_cast<LPTHREAD_START_ROUTINE>(::GetProcAddress(kernelModule, "LoadLibraryW")) : nullptr;
            HANDLE remoteLoadLibrary = loadLibrary ? ::CreateRemoteThread(process, nullptr, 0, loadLibrary, dllPathRemote, 0, nullptr) : nullptr;

            if (remoteLoadLibrary)
            {
                std::array<HANDLE, 3> handles = { remoteLoadLibrary, stopEvent, process };
                if (::WaitForMultipleObjects(static_cast<DWORD>(handles.size()), handles.data(), FALSE, INFINITE) == WAIT_OBJECT_0)
                {
                    if (::GetExitCodeThread(remoteLoadLibrary, reinterpret_cast<DWORD*>(&remoteModule)))
                    {
                        // Should be injected now...
                    }
                }

                ::CloseHandle(remoteLoadLibrary);
            }
        }

        BOOL freed = ::VirtualFreeEx(process, dllPathRemote, 0, MEM_RELEASE);
        assert(freed);
    }

    return remoteModule != nullptr;
}

// Use a helper process of opposite bitness to do the injection
static bool InjectDllOppositeBitness(_In_ HANDLE process, HANDLE stopEvent)
{
    // Handle must be transferable to a 32bit process
    assert((reinterpret_cast<size_t>(process) & 0xFFFFFFFF) == reinterpret_cast<size_t>(process));

    std::wstring exePath = DevInject::GetModuleFileName(DevInject::GetModule());
    size_t slash = exePath.rfind('\\');
    if (slash == std::wstring::npos)
    {
        assert(false);
        return false;
    }

    exePath.erase(slash + 1);
    exePath += L"DevInjector";
    exePath += ::IsMyProcess64 ? L"32" : L"64";
    exePath += L".exe";

    std::wstringstream commandLineBuffer;
    commandLineBuffer << L"\"" << exePath << L"\" " << ::GetProcessId(process) << L" " << reinterpret_cast<size_t>(process);
    std::wstring commandLine = commandLineBuffer.str();

    ULONG_PTR attributeListSize = 0;
    if (!::InitializeProcThreadAttributeList(nullptr, 1, 0, &attributeListSize) && ::GetLastError() != ERROR_INSUFFICIENT_BUFFER)
    {
        assert(false);
        return false;
    }

    std::vector<BYTE> attributeListData;
    attributeListData.resize(attributeListSize);
    LPPROC_THREAD_ATTRIBUTE_LIST attributeList = reinterpret_cast<LPPROC_THREAD_ATTRIBUTE_LIST>(attributeListData.data());

    if (!::InitializeProcThreadAttributeList(attributeList, 1, 0, &attributeListSize) || attributeList == nullptr ||
        !::UpdateProcThreadAttribute(attributeList, 0, PROC_THREAD_ATTRIBUTE_HANDLE_LIST, &process, sizeof(HANDLE), nullptr, nullptr))
    {
        assert(false);
        return false;
    }

    STARTUPINFOEX si{};
    si.StartupInfo.cb = sizeof(si);
    si.lpAttributeList = attributeList;

    PROCESS_INFORMATION pi;
    DWORD flags = CREATE_SUSPENDED | CREATE_UNICODE_ENVIRONMENT | EXTENDED_STARTUPINFO_PRESENT;
    bool status = false;

    if (::CreateProcess(nullptr, &commandLine[0], nullptr, nullptr, TRUE, flags, nullptr, nullptr, &si.StartupInfo, &pi))
    {
        ::ResumeThread(pi.hThread);
        ::WaitForSingleObject(pi.hProcess, INFINITE);

        DWORD exitCode;
        if (::GetExitCodeProcess(pi.hProcess, &exitCode))
        {
            status = (exitCode == 0);
        }

        ::CloseHandle(pi.hThread);
        ::CloseHandle(pi.hProcess);
    }

    return status;
}

bool DevInject::InjectDll(HANDLE process, HANDLE stopEvent, bool allowDifferentBitness)
{
    bool isOs64 = ::IsMyProcess64;
    if (!isOs64)
    {
        // Might be 32bit process on 64bit OS
        BOOL isWow64;
        isOs64 = ::IsWow64Process(::GetCurrentProcess(), &isWow64) && isWow64;
    }

    bool isOtherProcess64 = isOs64;
    if (isOtherProcess64)
    {
        // Might be 32bit process on 64bit OS
        BOOL isWow64;
        isOtherProcess64 = ::IsWow64Process(process, &isWow64) && !isWow64;
    }

    if (::IsMyProcess64 == isOtherProcess64)
    {
        return ::InjectDllSameBitness(process, stopEvent);
    }
    else if (allowDifferentBitness)
    {
        return ::InjectDllOppositeBitness(process, stopEvent);
    }

    return false;
}

void DevInject::CheckConsoleWindowSize(bool visibleOnly)
{
    HWND hwnd = ::GetConsoleWindow();
    if (hwnd && (!visibleOnly || ::IsWindowVisible(hwnd)))
    {
        HWND parent = ::GetParent(hwnd);
        if (parent)
        {
            RECT rect, rect2;
            if (::GetWindowRect(parent, &rect) && ::GetWindowRect(hwnd, &rect2) && ::memcmp(&rect, &rect2, sizeof(rect)))
            {
                ::SetWindowPos(hwnd, nullptr, 0, 0, rect.right - rect.left, rect.bottom - rect.top,
                    SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE | SWP_NOCOPYBITS);
            }
        }
    }
}

static DWORD __stdcall DetachThread(void* waitForWindowToRespond)
{
    HWND hwnd = reinterpret_cast<HWND>(waitForWindowToRespond);
    if (hwnd)
    {
        ::SendMessage(hwnd, WM_NULL, 0, 0);
    }

    ::FreeLibraryAndExitThread(DevInject::Dispose(), 0);
    return 0;
}

void DevInject::BeginDetach(HWND waitForWindowToRespond)
{
    // Don't use _beginthreadex since that will increment the DLL load count
    HANDLE thread = ::CreateThread(nullptr, 0, ::DetachThread, reinterpret_cast<void*>(waitForWindowToRespond), 0, nullptr);
    if (thread)
    {
        ::CloseHandle(thread);
    }
}
