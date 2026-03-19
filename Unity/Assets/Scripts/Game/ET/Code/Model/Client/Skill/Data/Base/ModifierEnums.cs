namespace ET.Client
{
    public enum ModifierOperation
    {
        Add,
        Multiply,
        Divide,
        Override
    }

    public enum ModifierMagnitudeSourceType
    {
        FixedValue,
        Formula,
        ModifierMagnitudeCalculation,
        SetByCaller
    }

    public enum MMCType
    {
        AttributeBased,
        LevelBased
    }
}
