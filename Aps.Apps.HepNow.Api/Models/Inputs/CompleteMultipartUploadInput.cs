namespace Aps.Apps.HepNow.Models.Inputs;

public class CompleteMultipartUploadInput : InputDef
{
    public string key { get; set; }
    public string uploadId { get; set; }
    public int expectedPartCount { get; set; }
}
