namespace Gorgon.BCnEncoder.Encoder;

/// <summary>
/// 
/// </summary>
public enum CompressionQuality
{
    /// <summary>
    /// Fast, but low quality. Especially bad with gradients.
    /// </summary>
    Fast,
    /// <summary>
    /// Strikes a balance between speed and quality. Good enough for most purposes.
    /// </summary>
    Balanced,
    /// <summary>
    /// Aims for best quality encoding. Can be very slow.
    /// </summary>
    BestQuality
}
