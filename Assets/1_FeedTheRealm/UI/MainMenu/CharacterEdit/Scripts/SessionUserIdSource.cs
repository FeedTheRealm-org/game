using System;

public class SessionUserIdSource : ICharacterIdSource
{
    public event Action<string> OnCharacterIdChanged;

    private Session.Session session;
    public string CharacterId => session?.UserID;

    public SessionUserIdSource(Session.Session session)
    {
        this.session = session;
    }
}
