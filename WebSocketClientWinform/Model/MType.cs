namespace UDPServerAndWebSocketClient
{
    public enum MType
    {
        JoinRequest ,
        JoinAccept,
        UnconfirmedDataUp,
        UnconfirmedDataDown,
        ConfirmedDataUp,
        ConfirmedDataDown,
        RejoinRequest,
        Proprietary

    }
}