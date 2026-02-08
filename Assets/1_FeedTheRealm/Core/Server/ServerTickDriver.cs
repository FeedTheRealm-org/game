using VContainer.Unity;

public class ServerTickDriver
{
    readonly GameLoop gameLoop;

    public ServerTickDriver(GameLoop gameLoop)
    {
        this.gameLoop = gameLoop;
    }

    public void TickOnce(float dt)
    {
        gameLoop.TickOnce(dt);
    }
}
