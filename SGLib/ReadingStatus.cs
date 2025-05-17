namespace SGLib;

internal enum ReadingStatus
{
    Successful,
    Errored,
    ReachedChunkEnd,
    ReachedFileEnd
}