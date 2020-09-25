using Include;
using System;
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

    public class Login : Message {
        public new MsgType type = MsgType.Login;
        public string email { get; set; }
        public string password { get; set; }
    }

    public class LoginResult : MessageResult {
        public new MsgType type = MsgType.LoginResult;
        public string email { get; set; }
        public string userName { get; set; }
        public string uniqueId { get; set; }
        public string bio { get; set; }
        public byte[] profile { get; set; }
    }

    public class MessageResult : Message
    {
        public MsgType callingType { get; set; }
        public Result result { get; set; }
        public string error { get; set; }

        public enum Result
        {
            Invalid = -1,
            Success = 0,
            Pending = 1,
            Failure = 3
        }
    }

    public class RegisterUser : Message
    {
        public new MsgType type = MsgType.RegisterUser;
        public string email { get; set; }
        public string password { get; set; }
        public string name { get; set; }
        public string bio { get; set; }
    }

    public class UpdateUserProfile : Message
    {
        public new MsgType type = MsgType.UpdateUserProfile;
        public string email { get; set; }
        public string name { get; set; }
        public string bio { get; set; }
        public byte[] profile { get; set; }
    }

    public class UpdateUserPassword : Message
    {
        public new MsgType type = MsgType.UpdateUserPassword;
        public string email { get; set; }
        public string oldPassword { get; set; }
        public string newPassword { get; set; }
    }

    public class SendFriendRequest : Message
    {
        public new MsgType type = MsgType.SendFriendRequest;
        public string fromEmail { get; set; }
        public string toEmail { get; set; }
    }

    public class AcceptFriend : Message
    {
        public new MsgType type = MsgType.AcceptFriend;
        public string fromEmail { get; set; }
        public string toEmail { get; set; }
    }

    public class AcceptFriendResult : MessageResult {
        public new MsgType type = MsgType.AcceptFriendResult;
        public string conversationId { get; set; }
    }

    public class GetMyFriendRequests : Message
    {
        public new MsgType type = MsgType.GetMyFriendRequests;
        public string email { get; set; }
    }

    public class GetMyFriendRequestsResult : MessageResult {
        public new MsgType type = MsgType.GetMyFriendRequestsResult;
        public string[] name { get; set; }
        public string[] userId { get; set; }
        public string[] email { get; set; }
    }

    public class GetFriendsRequestingMe : Message
    {
        public new MsgType type = MsgType.GetFriendsRequestingMe;
        public string email { get; set; }
    }

    public class GetFriendsRequestingMeResult : MessageResult {
        public new MsgType type = MsgType.GetFriendsRequestingMeResult;
        public string[] name { get; set; }
        public string[] userId { get; set; }
        public string[] email { get; set; }
    }

    public class GetFriends : Message
    {
        public new MsgType type = MsgType.GetFriends;
        public string email { get; set; }
    }

    public class GetFriendsResult : MessageResult {
        public new MsgType type = MsgType.GetFriendsResult;
        public string[] name { get; set; }
        public string[] userId { get; set; }
        public string[] email { get; set; }
        public string[] conversationId { get; set; }
    }

    public class SendMessage : Message
    {
        public new MsgType type = MsgType.SendMessage;
        public string email { get; set; }
        public string text { get; set; }
        public string conversationId { get; set; }
    }

    public class RetrieveMessages : Message
    {
        public new MsgType type = MsgType.RetrieveMessages;
        public string conversationId { get; set; }
        public Int64 dateTime { get; set; }
    }

    public class RetrieveMessagesResult : MessageResult {
        public new MsgType type = MsgType.RetrieveMessagesResult;
        public Chat[] chat { get; set; }
    }

    public class Chat {
        public string userId { get; set; }
        public string email { get; set; }
        public string text { get; set; }
        public Int64 dateTime { get; set; }
    }

    public class RetrieveUserProfile : Message
    {
        public new MsgType type = MsgType.RetrieveUserProfile;
        public string email { get; set; }
    }

    public class RetrieveUserProfileResult : MessageResult {
        public new MsgType type = MsgType.RetrieveUserProfileResult;
        public string userId { get; set; }
        public string email { get; set; }
        public string name { get; set; }
        public string bio { get; set; }
        public byte[] profile { get; set; }
    }

    public class GetMatchDetails : Message
    {
        public new MsgType type = MsgType.GetMatchDetails;
        public string matchId { get; set; }
    }

    public class GetMatchDetailsResult : MessageResult {
        public new MsgType type = MsgType.GetMatchDetailsResult;
        public string matchId { get; set; }
        public string email_1 { get; set; }
        public string email_2 { get; set; }
        public string userId_1 { get; set; }
        public string userId_2 { get; set; }
        public string board { get; set; }
        public Int64 dateTime { get; set; } //temp string as that is how the database function returns it but should probably convert
        public string ended { get; set; }
    }

    public class CreateMatch : Message
    {
        public new MsgType type = MsgType.CreateMatch;
        public string email_1 { get; set; }
        public string email_2 { get; set; }
    }

    public class CreateMatchResult : MessageResult
    {
        public new MsgType type = MsgType.CreateMatchResult;
        public string matchId { get; set; }
        public string conversationId { get; set; }
    }

    public class GetMatchHistory : Message
    {
        public new MsgType type = MsgType.GetMatchHistory;
        public string email_1 { get; set; }
    }

    public class GetMatchHistoryResult : MessageResult
    {
        public new MsgType type = MsgType.GetMatchHistoryResult;
        public string[] userId { get; set; }
        public string[] email { get; set; }
        public string[] matchId { get; set; }
        public string[] board { get; set; }
        public Int64[] startDateTime { get; set; } //temp string as that is how the database function returns it but should probably convert
        public bool[] ended { get; set; }
    }


    public enum MsgType : int {
        Ack = 0,
        Heartbeat = 1,

        Disconnect = 2,   
        Initialise = 3,		
        InitialiseResult = 4,	

        RegisterUser = 5,
        MessageResult = 6,

        UpdateUserProfile = 7,
        UpdateUserPassword = 8,

        SendFriendRequest = 9,
        AcceptFriend = 10,
        AcceptFriendResult = 11,

        GetMyFriendRequests = 12,
        GetMyFriendRequestsResult = 13,

        GetFriendsRequestingMe = 14,
        GetFriendsRequestingMeResult = 15,

        GetFriends = 16,
        GetFriendsResult = 17,

        SendMessage = 18,
        RetrieveMessages = 19,
        RetrieveMessagesResult = 20,


        RetrieveUserProfile = 21,
        RetrieveUserProfileResult = 22,

        GetMatchDetails = 23,
        GetMatchDetailsResult = 24,

        Login = 25,
        LoginResult = 26,

        EnterMatchQueue = 27,	
        MatchFound = 28,	   
        SendChallenge = 29,

        CreateMatch = 30,
        CreateMatchResult = 31,

        GetMatchHistory = 32,
        GetMatchHistoryResult = 33,

        Unknown = int.MaxValue
    }







}
