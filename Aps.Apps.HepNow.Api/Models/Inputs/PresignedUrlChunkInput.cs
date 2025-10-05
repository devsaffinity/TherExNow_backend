namespace Aps.Apps.HepNow.Models.Inputs;

public class PresignedUrlChunkInput : InputDef
{
    public string key { get; set; }
    public int ChunkCount { get; set; }
}
