/* Minimal Media Foundation probe: open a URL like Unity's VideoPlayer does.
 * Built as a Winelib app with the portable Wine build so it exercises the
 * exact same builtin mfreadwrite/mfplat as the game. */
#define COBJMACROS
#include <windows.h>
#include <wchar.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mfreadwrite.h>
#include <stdio.h>

int wmain(int argc, WCHAR **argv)
{
    HRESULT hr;
    IMFSourceReader *reader = NULL;

    if (argc < 2)
    {
        printf("usage: mfprobe <url-or-path>\n");
        return 2;
    }

    hr = MFStartup(MF_VERSION, MFSTARTUP_FULL);
    printf("MFStartup: 0x%08lx\n", (unsigned long)hr); fflush(stdout);

    hr = MFCreateSourceReaderFromURL(argv[1], NULL, &reader);
    printf("MFCreateSourceReaderFromURL: 0x%08lx\n", (unsigned long)hr); fflush(stdout);

    if (SUCCEEDED(hr) && reader)
    {
        IMFMediaType *mt = NULL;
        hr = IMFSourceReader_GetNativeMediaType(reader,
                (DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, &mt);
        printf("GetNativeMediaType(video): 0x%08lx\n", (unsigned long)hr); fflush(stdout);
        if (SUCCEEDED(hr) && mt)
        {
            GUID subtype;
            if (SUCCEEDED(IMFMediaType_GetGUID(mt, &MF_MT_SUBTYPE, &subtype)))
                printf("  subtype: %08lx-%04x-%04x\n",
                        (unsigned long)subtype.Data1, subtype.Data2, subtype.Data3); fflush(stdout);
            IMFMediaType_Release(mt);
        }

        IMFSample *sample = NULL;
        DWORD flags = 0, actual = 0;
        hr = IMFSourceReader_ReadSample(reader, (DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                0, &actual, &flags, NULL, &sample);
        printf("ReadSample: 0x%08lx flags=0x%lx", (unsigned long)hr, (unsigned long)flags);
        if (sample)
        {
            DWORD len = 0;
            IMFSample_GetTotalLength(sample, &len);
            printf(" len=%lu", (unsigned long)len);
            IMFSample_Release(sample);
        }
        printf("\n"); fflush(stdout);
        IMFSourceReader_Release(reader);
    }

    MFShutdown();
    return SUCCEEDED(hr) ? 0 : 1;
}
