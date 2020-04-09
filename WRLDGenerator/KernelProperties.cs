
/// <summary>
/// The class for properties we want to access for each
/// kernel to constract our models with later
/// </summary>
[System.Serializable]
public class KernelProperties
{

    // Currently we just support the elevation of the individual structure
    public float heightLevel;
    
    public KernelProperties(float level)
    {
        heightLevel = level;
    }
}