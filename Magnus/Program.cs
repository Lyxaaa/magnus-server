using DatabaseConnection;
using System;
using static Magnus.MessageResult;
using Msg = Include.Json.Message;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Magnus {
    class Program {

        static object lockObj = new object();

        static void Main(string[] args) {
            var database = new Database();
            var server = new Server(null, 2457);

            server.OnReceiveListener += (clientId, socketType, dataType, data) =>
            {
                #region Login
                if (Msg.TryCast(dataType, data, (int)MsgType.Login, out Login login))
                {
                    var result = database.GetSelectUserProfile(login.email);

                    if (result.Item1 != login.email)
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Failure,
                            error = "invalid email or password"
                        });
                    }
                    else if (result.Item2 == login.password)
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Success,
                            email = result.Item1,
                            uniqueId = HashString(result.Item1),
                            userName = result.Item3,
                            bio = result.Item4
                            //profile = result.Item5 // < this is a file directory, convert this into bytes and then send it
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Failure,
                            error = "invalid password or email"
                        });
                    }
                }
                #endregion
                #region RetrieveUserProfile
                else if (Msg.TryCast(dataType, data, (int)MsgType.RetrieveUserProfile, out RetrieveUserProfile retrieveuserprofile))
                {
                    var result = database.GetSelectUserProfile(retrieveuserprofile.email);
                    if (result.Item1 != retrieveuserprofile.email)
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Failure,
                            error = "invalid email or password"
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Success,
                            email = result.Item1,
                            uniqueId = HashString(result.Item1),
                            userName = result.Item3,
                            bio = result.Item4
                            //profile = result.Item5 // < this is a file directory, convert this into bytes and then send it
                        });
                    }
                }
                #endregion
                #region RegisterUser
                else if (Msg.TryCast(dataType, data, (int)MsgType.RegisterUser, out RegisterUser registeruser))
                {
                    var result = database.InsertUser(registeruser.email, registeruser.password, registeruser.name, registeruser.bio, "Profile pic placeholder");
                    if (result)
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Success,
                            email = registeruser.email,
                            uniqueId = HashString(registeruser.email),
                            userName = registeruser.name,
                            bio = registeruser.bio
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Failure,
                            error = "insert failed see database log for details",
                            callingType = MsgType.RegisterUser
                        });
                    }
                }
                #endregion
                #region UpdateUserProfile
                else if (Msg.TryCast(dataType, data, (int)MsgType.UpdateUserProfile, out UpdateUserProfile updateuserprofile))
                {
                    var prior = database.GetSelectUserProfile(updateuserprofile.email);
                    var newName = updateuserprofile.name;
                    var newBio = updateuserprofile.bio;
                    if (String.IsNullOrEmpty(newName))
                    {
                        newName = prior.Item3;
                    }

                    if (String.IsNullOrEmpty(newBio))
                    {
                        newBio = prior.Item4;
                    }

                    var result = database.UpdateUser(updateuserprofile.email, prior.Item2, newName, updateuserprofile.bio, "Profile pic placeholder");

                    if (result)
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.UpdateUserProfile
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Failure,
                            error = "update failed see database log for details",
                            callingType = MsgType.UpdateUserProfile
                        });
                    }
                }
                #endregion
                #region UpdateUserPassword
                else if (Msg.TryCast(dataType, data, (int)MsgType.UpdateUserPassword, out UpdateUserPassword updateuserpassword))
                {
                    var prior = database.GetSelectUserProfile(updateuserpassword.email);
                    if (updateuserpassword.oldPassword == prior.Item2)
                    {
                        var result = database.UpdateUser(updateuserpassword.email, updateuserpassword.newPassword, prior.Item3, prior.Item4, prior.Item5);
                        if (result)
                        {
                            server.SendToClient(clientId, new MessageResult()
                            {
                                result = Result.Success,
                                callingType = MsgType.UpdateUserPassword
                            });
                        }
                        else
                        {
                            server.SendToClient(clientId, new MessageResult()
                            {
                                result = Result.Failure,
                                error = "update failed see database log for details",
                                callingType = MsgType.UpdateUserPassword
                            });
                        }
                    }
                    else
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Failure,
                            error = "Incorrect Password",
                            callingType = MsgType.UpdateUserPassword
                        });
                    }
                }
                #endregion
                #region SendFriendRequest
                else if (Msg.TryCast(dataType, data, (int)MsgType.SendFriendRequest, out SendFriendRequest sendfriendrequest))
                {
                    var result = database.InsertFriendRequest(sendfriendrequest.fromEmail, sendfriendrequest.toEmail);
                    if (result)
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.SendFriendRequest
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.SendFriendRequest
                        });
                    }
                }

                #endregion

                #region AcceptFriend
                else if (Msg.TryCast(dataType, data, (int)MsgType.AcceptFriend, out AcceptFriend acceptfriend))
                {
                    var result = database.InsertFriend(acceptfriend.fromEmail, acceptfriend.toEmail);
                    var id = database.GetConversationsBetween(acceptfriend.fromEmail, acceptfriend.toEmail);
                    if (result)
                    {
                        server.SendToClient(clientId, new AcceptFriendResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.AcceptFriend,
                            conversationId = id[1].Item1 //assuming only 1 conversation between 2 users
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new AcceptFriendResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.AcceptFriend
                        });
                    }
                }

                #endregion GetMyFriendRequests
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetMyFriendRequests, out GetMyFriendRequests getmyfriendrequests))
                {
                    var result = database.GetUserRequestSent(getmyfriendrequests.email);
                    if (result)
                    {
                        server.SendToClient(clientId, new AcceptFriendResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.AcceptFriend,
                            conversationId = id[1].Item1 //assuming only 1 conversation between 2 users
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new AcceptFriendResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.AcceptFriend
                        });
                    }
                }
                #region
                #endregion
                #region
                #endregion
                #region
                #endregion
                #region
                #endregion
                #region
                #endregion
                else
                {
                    server.SendToClient(clientId, new MessageResult()
                    {
                        result = Result.Invalid,
                        error = "error or listener not implamanted ",
                        callingType = MsgType.SendFriendRequest
                    });
                }
                };

                

            server.Begin();
            
            Console.ReadKey();
            server.End();
        }

        //hash for user id
        public static string HashString(string text)
        {
            const string chars = "0123456789abcdefghijklmnopqrztuvABCDEFGHIJKLMNOPQRSTUVWXYZ";
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);

            char[] hash2 = new char[32];

            // Note that here we are wasting bits of hash! 
            // But it isn't really important, because hash.Length == 32
            for (int i = 0; i < hash2.Length; i++)
            {
                hash2[i] = chars[hash[i] % chars.Length];
            }

            return new string(hash2);
        }
    }
}
