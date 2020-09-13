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
        public Result result;
        public string error;
        public string userName;
        public string uniqueId;
        public string bio;
        public byte[] profile;
    }

    public enum Result : int {
        Success, Pending, Failure
    }

    public enum MsgType : int {
        Ack,
        Heartbeat,

        Initialise,
        InitialiseResult,

        RegisterUser,
        RegisterUserResult,

        UpdateUserProfile,

        SendFriendRequest,
        AddFriend,

        GetMyFriendRequests,
        GetFriendsRequestingMe,

        GetFriends,

        SendMessage,

        RetrieveUserProfile,

        GetMatchDetails,

        Login,
        LoginResult,

        EnterMatchQueue,
        MatchFound,


        Disconnect,

        Unknown = int.MaxValue
    }
}
