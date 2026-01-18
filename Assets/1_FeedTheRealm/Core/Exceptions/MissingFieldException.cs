namespace Game.Core.Exceptions;

public class MissingFieldException : System.Exception
{
    public MissingFieldException(string msg)
        : base(msg) { }

    public MissingFieldException(string fieldName, string className)
        : base(
            $"The field '{fieldName}' is missing in the class '{className}'. Please ensure it is properly assigned."
        ) { }
}
