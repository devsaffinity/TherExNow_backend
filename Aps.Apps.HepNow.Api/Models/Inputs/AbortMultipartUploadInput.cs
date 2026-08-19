namespace Aps.Apps.HepNow.Models.Inputs;

public class AbortMultipartUploadInput : InputDef
{
    public string key { get; set; }
    public string uploadId { get; set; }
}
