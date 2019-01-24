#include "stdafx.h"
#include <winsatcominterfacei.h>
#include <conio.h>  // For kbhit()
#pragma comment(lib, "ole32.lib")

// Class that implements IWinSATInitiateEvents. Implement this class to
// get progress information and completion notification.
class CWinSATCallbacks : public IWinSATInitiateEvents
{
	LONG m_lRefCount;

public:

	// Constructor, Destructor
	CWinSATCallbacks() { m_lRefCount = 1; };
	~CWinSATCallbacks() {};

	// IUnknown methods
	HRESULT __stdcall QueryInterface(REFIID riid, LPVOID *ppvObj);
	ULONG __stdcall AddRef();
	ULONG __stdcall Release();

	// IWinSATInitiateEvents methods
	HRESULT __stdcall WinSATComplete(HRESULT hr, LPCWSTR description);
	HRESULT __stdcall WinSATUpdate(UINT currentTick, UINT tickTotal, LPCWSTR currentState);
};


HRESULT CWinSATCallbacks::QueryInterface(REFIID riid, LPVOID* ppvObj)
{
	if (riid == __uuidof(IUnknown) || riid == __uuidof(IWinSATInitiateEvents))
	{
		*ppvObj = this;
	}
	else
	{
		*ppvObj = NULL;
		return E_NOINTERFACE;
	}

	AddRef();
	return NOERROR;
}

ULONG CWinSATCallbacks::AddRef()
{
	return InterlockedIncrement(&m_lRefCount);
}

ULONG CWinSATCallbacks::Release()
{
	ULONG  ulCount = InterlockedDecrement(&m_lRefCount);

	if (0 == ulCount)
	{
		delete this;
	}

	return ulCount;
}

// Is called when WinSAT completes the assessment or an error occurs.
HRESULT CWinSATCallbacks::WinSATComplete(HRESULT hr, LPCWSTR description)
{
	if (SUCCEEDED(hr))
	{
		wprintf(L"\n*** %s", description);
	}
	else
	{
		wprintf(L"\n*** The assessment failed with 0x%x (%s)\n", hr, description);
	}

	return S_OK;
}

// Is called when the assessment makes progress. Indicates the percentage of the assessment
// that is complete and the current component being assessed.
HRESULT CWinSATCallbacks::WinSATUpdate(UINT currentTick, UINT tickTotal, LPCWSTR currentState)
{
	// Typically, you would provide the tick values to a ProgressBar control.

	if (tickTotal > 0)
	{
		wprintf(L"\n*** Percent complete: %u%%\n", 100 * currentTick / tickTotal);
		wprintf(L"*** Currently assessing: %s\n\n", currentState);
	}

	return S_OK;
}

int main(void)
{
	HRESULT hr = S_OK;
	IInitiateWinSATAssessment* pAssessment = NULL;
	CWinSATCallbacks* pCallbacks = NULL;  // Class that implements IWinSATInitiateEvents
	int returnCode = 0;

	//wprintf(L"\n*** Attach your debugger and press any key to continue.");
	//while (!_kbhit())
	//Sleep(10);

	//BOOL f = AllocConsole();
	//FILE *file = nullptr;
	//freopen_s(&file, "CONIN$", "r", stdin);
	//freopen_s(&file, "CONOUT$", "w", stdout);

	//printf_s("this is a test of the console window.");
	//while(!_kbhit())
	//	Sleep(10);

	CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);

	// Get an instance of the assessment interface.
	hr = CoCreateInstance(__uuidof(CInitiateWinSAT),
		NULL,
		CLSCTX_INPROC_SERVER,
		__uuidof(IInitiateWinSATAssessment),
		(void**)&pAssessment);

	if (FAILED(hr))
	{
		wprintf(L"Failed to create an instance of IInitiateWinSATAssessment. Failed with 0x%x.\n", hr);
		goto cleanup;
	}

	wprintf(L"Running formal assessment... hit any key when complete.\n");

	pCallbacks = new CWinSATCallbacks();
	if (NULL == pCallbacks)
	{
		wprintf(L"Failed to create an instance of the CWinSATCallbacks class.\n");
		returnCode = 1;
		goto cleanup;
	}

	// Run the formal assessment.
	hr = pAssessment->InitiateFormalAssessment(pCallbacks, NULL);
	if (FAILED(hr))
	{
		// This is a failure to start WinSAT. If WinSAT fails while running, 
		// your implementation of the IWinSATInitiateEvents::WinSATComplete 
		// method will receive the failure code.
		wprintf(L"InitiateFormalAssessment failed with 0x%x.\n", hr);
		returnCode = 2;

		goto cleanup;
	}

	//while (!_kbhit())
	//	Sleep(10);

cleanup:

	if (pAssessment)
		pAssessment->Release();

	if (pCallbacks)
		pCallbacks->Release();

	CoUninitialize();

	return returnCode;
}