using Include;
using System.Collections.Generic;
using I = Include.Json;

namespace Magnus {
    public class Message : I.Message {
        public new MsgType type {
            get { return (MsgType)base.type; }
            set { base.type = (int)value; }
        }

        public static bool TryCast(DataType dataType, object data, int msgType) {
            return TryCast(dataType, data, msgType, out Message _);
        }

        public byte[] GetBytes() {
            return base.GetBytes();
        }
    }

    public class Ack : Message {
        public new MsgType type = MsgType.Ack;
        public MsgType acknowledging;
        public long oldTimeStamp;
    }

    public class Initialise : Message {
        public new MsgType type = MsgType.Initialise;
        public string id { get; set; }
    }

    public class InitialiseResult : Message {
        public new MsgType type = MsgType.InitialiseResult;
        public Result result { get; set; }
        public string error { get; set; }
    }

    public class Login : Message {
        public new MsgType type = MsgType.Login;
        public string email { get; set; }
        public string password { get; set; }
    }

    public class LoginResult : Message {
        public new MsgType type = MsgType.LoginResult;
        public Result result { get; set; }
        public string error { get; set; }
        public string userName { get; set; }
        public string uniqueId { get; set; }
        public string bio { get; set; }
        public byte[] profile { get; set; }
    }

    public enum Result : int {
        Success = 0, Pending = 1, Failure = 2
    }

    public enum MsgType : int
    {
        Ack = 0,
        Heartbeat = 1,

        Initialise = 2,
        InitialiseResult = 3,

        RegisterUser = 4,
        RegisterUserResult = 5,

        UpdateUserProfile = 6,

        SendFriendRequest = 7,
        AcceptFriend = 8,
        AcceptFriendResult = 9,

        GetMyFriendRequests = 10,
        GetMyFriendRequestsResult = 11,

        GetFriendsRequestingMe = 12,
        GetFriendsRequestingMeResult = 13,

        GetFriends = 14,
        GetFriendsResult = 15,

        SendMessage = 16,
        RetrieveMessages = 17,
        RetrieveMessagesResult = 18,


        RetrieveUserProfile = 19,
        RetrieveUserProfileResult = 20,

        GetMatchDetails = 21,

        Login = 22,
        LoginResult = 23,

        EnterMatchQueue = 24,
        MatchFound = 25,


        Disconnect = 26,

        UpdateUserPassword = 27,
        CreateMatch = 28,
        CreateMatchResult = 29,


        Unknown = int.MaxValue
    }



}
