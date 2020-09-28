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
            var playqueue = new List<Tuple<string, string>>();//cliantid, email
            Dictionary<string, string> emailtoclientid = new Dictionary<string, string>();
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
                        emailtoclientid.Add(result.Item1, clientId);
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
                            callingType = MsgType.UpdateUserProfile,
                            type = MsgType.MessageResult
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Failure,
                            error = "update failed see database log for details",
                            callingType = MsgType.UpdateUserProfile,
                            type = MsgType.MessageResult
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
                                callingType = MsgType.UpdateUserPassword,
                                type = MsgType.MessageResult
                            });
                        }
                        else
                        {
                            server.SendToClient(clientId, new MessageResult()
                            {
                                result = Result.Failure,
                                error = "update failed see database log for details",
                                callingType = MsgType.UpdateUserPassword,
                                type = MsgType.MessageResult
                            });
                        }
                    }
                    else
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Failure,
                            error = "Incorrect Password",
                            callingType = MsgType.UpdateUserPassword,
                            type = MsgType.MessageResult
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
                            callingType = MsgType.SendFriendRequest,
                            type = MsgType.MessageResult
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.SendFriendRequest,
                            type = MsgType.MessageResult
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
                            conversationId = id
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
                #region GetMyFriendRequests
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetMyFriendRequests, out GetMyFriendRequests getmyfriendrequests))
                {
                    var result = database.GetUserRequestSent(getmyfriendrequests.email);
                    if (result.Count >= 1)
                    {
                        var returnemail = new List<String>();
                        var returnname = new List<String>();
                        var returnuserid = new List<String>();
                        for (int i = 1; i < result.Count; i++)
                        {
                            returnemail.Add(result[i].Item1);
                            returnname.Add(result[i].Item3);
                            returnuserid.Add(HashString(result[i].Item1));
                        }
                        server.SendToClient(clientId, new GetMyFriendRequestsResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.GetMyFriendRequests,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            name = returnname.ToArray()

                        });
                    }
                    else if (result.Count == 0)
                    {
                        server.SendToClient(clientId, new GetFriendsResult()
                        {
                            result = Result.Failure,
                            error = "zero results returned",
                            callingType = MsgType.GetMyFriendRequests
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new GetMyFriendRequestsResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetMyFriendRequests
                        });
                    }
                }
                #endregion
                #region GetFriendsRequestingMe
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetFriendsRequestingMe, out GetFriendsRequestingMe getfriendsrequestingme))
                {
                    var result = database.GetUserRequestRecived(getfriendsrequestingme.email);
                    if (result.Count >= 1)
                    {
                        var returnemail = new List<String>();
                        var returnname = new List<String>();
                        var returnuserid = new List<String>();
                        for (int i = 1; i < result.Count; i++)
                        {
                            returnemail.Add(result[i].Item1);
                            returnname.Add(result[i].Item3);
                            returnuserid.Add(HashString(result[i].Item1));
                        }
                        server.SendToClient(clientId, new GetFriendsRequestingMeResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.GetFriendsRequestingMe,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            name = returnname.ToArray()

                        });
                    }
                    else if (result.Count == 0)
                    {
                        server.SendToClient(clientId, new GetFriendsResult()
                        {
                            result = Result.Failure,
                            error = "zero results returned",
                            callingType = MsgType.GetFriendsRequestingMe
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new GetFriendsRequestingMeResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetFriendsRequestingMe
                        });
                    }
                }
                #endregion
                #region GetFriends
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetFriends, out GetFriends getfriends))
                {
                    var result = database.GetUserFriends(getfriends.email);
                    if (result.Count >= 1)
                    {
                        var returnemail = new List<String>();
                        var returnname = new List<String>();
                        var returnuserid = new List<String>();
                        var returnconversationid = new List<String>();
                        for (int i = 1; i < result.Count; i++)
                        {
                            returnemail.Add(result[i].Item1);
                            returnname.Add(result[i].Item2);
                            returnuserid.Add(HashString(result[i].Item1));
                            //this will be removed lates and is a temporary way to get conversation ID
                            returnconversationid.Add(database.GetConversationsBetween(getfriends.email, (result[i].Item1)));
                        }
                        server.SendToClient(clientId, new GetFriendsResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.GetFriends,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            name = returnname.ToArray(),
                            conversationId = returnconversationid.ToArray()

                        });
                    }
                    else if (result.Count == 0)
                    {
                        server.SendToClient(clientId, new GetFriendsResult()
                        {
                            result = Result.Failure,
                            error = "zero results returned",
                            callingType = MsgType.GetFriends
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new GetFriendsResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetFriends
                        });
                    }
                }
                #endregion
                #region SendMessage
                else if (Msg.TryCast(dataType, data, (int)MsgType.SendMessage, out SendMessage sendmessage))
                {
                    var result = database.InsertMessage(sendmessage.conversationId, sendmessage.email, sendmessage.text);
                    if (result)
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.SendMessage,
                            type = MsgType.MessageResult
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new MessageResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.SendMessage,
                            type = MsgType.MessageResult
                        });
                    }
                }
                #endregion
                #region RetrieveMessages
                else if (Msg.TryCast(dataType, data, (int)MsgType.RetrieveMessages, out RetrieveMessages retrievemessages))
                {
                    var result = database.GetMessages(retrievemessages.conversationId);
                    if (result.Count >= 1)
                    {
                        var returnchat = new List<Chat>();
                        var numberorerror = 0;
                        for (int i = 1; i < result.Count; i++)
                        {
                            try
                            {
                                returnchat.Add(new Chat()
                                {
                                    text = result[i].Item1,
                                    dateTime = long.Parse(result[i].Item2),
                                    email = result[i].Item3,
                                    userId = HashString(result[i].Item3)

                                });
                            }
                            catch (Exception e)
                            {
                                numberorerror++;
                            }
                        }
                        server.SendToClient(clientId, new RetrieveMessagesResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.RetrieveMessages,
                            chat = returnchat.ToArray(),
                            error = numberorerror.ToString()

                        });
                    }
                    else if (result.Count == 0)
                    {
                        server.SendToClient(clientId, new RetrieveMessagesResult()
                        {
                            result = Result.Failure,
                            error = "zero results returned",
                            callingType = MsgType.RetrieveMessages
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new RetrieveMessagesResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.RetrieveMessages
                        });
                    }
                }
                #endregion
                #region GetMatchDetails
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetMatchDetails, out GetMatchDetails getmatchdetails))
                {
                    var result = database.GetMatch(getmatchdetails.matchId);
                    if (result.Item1 != getmatchdetails.matchId)
                    {
                        server.SendToClient(clientId, new GetMatchDetailsResult()
                        {
                            result = Result.Failure,
                            error = "query match id did not match requested match id (possibly zero results)"
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new GetMatchDetailsResult()
                        {
                            result = Result.Success,
                            dateTime = long.Parse(result.Item2),
                            matchId = result.Item1,
                            ended = result.Item3,
                            board = result.Item4,
                            userId_1 = HashString(result.Item5),
                            email_1 = result.Item5,
                            email_2 = result.Item6,
                            userId_2 = HashString(result.Item6)

                        });
                    }
                }
                #endregion
                //do we need to send message to both cliants and if so do we need additional fields on the message to include cliant id or alter the database?
                #region CreateMatch 
                else if (Msg.TryCast(dataType, data, (int)MsgType.CreateMatch, out CreateMatch creatematch))
                {
                    var result = database.InsertMatch(creatematch.email_1, creatematch.email_2);
                    //get conversation ID or create if non exist
                    var coversation = database.GetConversationsBetween(creatematch.email_1, creatematch.email_2);
                    if (coversation == "invalid")
                    {
                        database.InsertConversation(creatematch.email_1, creatematch.email_2);
                        coversation = database.GetConversationsBetween(creatematch.email_1, creatematch.email_2);
                    }
                    if (!String.IsNullOrEmpty(result))
                    {
                        if (emailtoclientid.ContainsKey(creatematch.email_1)) { 

                            server.SendToClient(emailtoclientid[creatematch.email_1], new CreateMatchResult()
                            {
                                result = Result.Success,
                                callingType = MsgType.CreateMatch,
                                matchId = result,
                                conversationId = coversation
                            });

                        }

                        if (emailtoclientid.ContainsKey(creatematch.email_2))
                        {
                            server.SendToClient(emailtoclientid[creatematch.email_2], new CreateMatchResult()
                            {
                                result = Result.Success,
                                callingType = MsgType.CreateMatch,
                                matchId = result,
                                conversationId = coversation
                            });
                        }
                    }
                    else
                    {
                        server.SendToClient(clientId, new CreateMatchResult()
                        {
                            result = Result.Failure,
                            error = "match request failed see database log for details",
                            callingType = MsgType.CreateMatch
                        });
                    }
                }
                #endregion 
                #region GetMatchHistory
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetMatchHistory, out GetMatchHistory getmatchhistory))
                {
                    var result = database.GetUserMatchHistory(getmatchhistory.email_1);
                    if (result.Count >= 1)
                    {
                        var returnemail = new List<String>();
                        var returnuserid = new List<String>();
                        var returnmatchid = new List<String>();
                        var returnboard = new List<String>();
                        var returnended = new List<bool>();
                        var returnstartdate = new List<Int64>();
                        for (int i = 1; i < result.Count; i++)
                        {
                            returnemail.Add(result[i].Item3);
                            returnuserid.Add(HashString(result[i].Item3));
                            returnmatchid.Add(result[i].Item2);
                            returnstartdate.Add(long.Parse(result[i].Item1));
                            returnboard.Add(result[i].Item5);
                        }
                        server.SendToClient(clientId, new GetMatchHistoryResult()
                        {
                            result = Result.Success,
                            callingType = MsgType.GetMatchHistory,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            board = returnboard.ToArray(),
                            startDateTime = returnstartdate.ToArray(),
                            ended = returnended.ToArray()

                        });
                    }
                    else if (result.Count == 0)
                    {
                        server.SendToClient(clientId, new GetMatchHistoryResult()
                        {
                            result = Result.Failure,
                            error = "zero results returned",
                            callingType = MsgType.GetMatchHistory
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new GetFriendsResult()
                        {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetFriends
                        });
                    }
                }
                #endregion
                #region EnterMatchQueue
                else if (Msg.TryCast(dataType, data, (int)MsgType.EnterMatchQueue, out EnterMatchQueue entermatchqueue))
                {
                    if (playqueue.Count >= 1)
                    {
                        var opponent = playqueue[1];
                        playqueue.Remove(opponent);
                        var result = database.InsertMatch(entermatchqueue.email, opponent.Item2);
                        //get conversation ID or create if non exist
                        var coversation = database.GetConversationsBetween(entermatchqueue.email, opponent.Item2);
                        {
                            database.InsertConversation(creatematch.email_1, creatematch.email_2);
                            coversation = database.GetConversationsBetween(creatematch.email_1, creatematch.email_2);
                        }
                        if (!String.IsNullOrEmpty(result))
                        {
                            server.SendToClient(clientId, new MatchFound()
                            {
                                result = Result.Success,
                                callingType = MsgType.EnterMatchQueue,
                                matchId = result,
                                conversationId = coversation,
                                opponentemail = opponent.Item2,
                                youremail = entermatchqueue.email
                            });

                            server.SendToClient(opponent.Item1, new MatchFound()
                            {
                                result = Result.Success,
                                callingType = MsgType.EnterMatchQueue,
                                matchId = result,
                                conversationId = coversation,
                                opponentemail = entermatchqueue.email,
                                youremail = opponent.Item2
                            });
                        }
                        else
                        {
                            server.SendToClient(opponent.Item1, new MatchFound()
                            {
                                result = Result.Failure,
                                error = "something went wrong during matchmaking",
                                callingType = MsgType.EnterMatchQueue,
                                type = MsgType.MatchFound
                            });
                        }
                    }
                    else
                    {
                        playqueue.Add(Tuple.Create(clientId, entermatchqueue.email));

                        server.SendToClient(clientId, new MatchFound()
                        {
                            result = Result.Pending,
                            error = "uou are in the queue",
                            callingType = MsgType.EnterMatchQueue
                        });
                        server.SendToClient(clientId, new MatchFound()
                        {
                            result = Result.Pending,
                            error = "uou are in the queue",
                            callingType = MsgType.EnterMatchQueue
                        });
                    }
                }
                #endregion
                #region SendChallenge
                else if (Msg.TryCast(dataType, data, (int)MsgType.SendChallenge, out SendChallenge sendchallenge))
                {
                    server.SendToClient(clientId, new MessageResult()
                    {
                        result = Result.Invalid,
                        error = "error or listener not implamanted ",
                        callingType = MsgType.SendFriendRequest,
                        type = MsgType.MessageResult
                    });
                }
                #endregion
                else
                {
                    server.SendToClient(clientId, new MessageResult()
                    {
                        result = Result.Invalid,
                        error = "error or listener not implamanted ",
                        callingType = MsgType.SendFriendRequest,
                        type = MsgType.MessageResult
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
