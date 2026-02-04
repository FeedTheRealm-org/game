namespace Game.Core.Exceptions;

public class MissingControllerException : System.Exception
{
    public MissingControllerException(string msg)
        : base(msg) { }

    public MissingControllerException(string gameObjectName, string controllerName)
        : base(
            $"The Game Object '{gameObjectName}' is missing the controller '{controllerName}'. Please ensure it is properly assigned."
        ) { }
}
