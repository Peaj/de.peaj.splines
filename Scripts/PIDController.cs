[System.Serializable]
public class PIDController
{
    public float Scale = 1f;
    public float PFactor, IFactor, DFactor;

    public float PError = 0f;
    public float IError = 0f;
    public float DError = 0f;

    private float integral;
    private float lastError;

    public PIDController(float pFactor, float iFactor, float dFactor)
    {
        this.PFactor = pFactor;
        this.IFactor = iFactor;
        this.DFactor = dFactor;
    }

    public float Update(float currentError, float deltaTime)
    {
        integral += currentError * deltaTime;
        var deriv = (currentError - lastError) / deltaTime;
        lastError = currentError;

        this.PError = currentError;
        this.IError = integral;
        if(deriv != 0) this.DError = deriv;

        return (currentError * PFactor
            + integral * IFactor
            + deriv * DFactor) * this.Scale;
    }
}