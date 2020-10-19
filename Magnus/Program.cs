using DatabaseConnection;
using Include.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using static Magnus.MessageResult;
using Msg = Include.Json.Message;

namespace Magnus {
    class Program {

        static object lockObj = new object();

        static void Main(string[] args) {
            var database = new Database();
            var server = new Server(null, 2457);
            var playqueue = new List<string>();//email

            var directory = "C:\\images\\";
            Dictionary<string, string> emailtoclientid = new Dictionary<string, string>();

            /*
             * some testing
            var myresult = database.GetAllOtherUserProfile("test123", "M",1,1);
            for (int i = 0; i < myresult.Count; i++) {
            }
            */
            server.OnDisconnectListener += (clientId) => {
                foreach (var item in emailtoclientid.Where(kvp => kvp.Value == clientId).ToList()) {
                    emailtoclientid.Remove(item.Key);
                }
            };
            server.OnReceiveListener += (clientId, socketType, dataType, data) => {
                Log.I("got message");
                #region Login
                if (Msg.TryCast(dataType, data, (int)MsgType.Login, out Login login)) {
                    Log.D("login");
                    var result = database.GetSelectUserProfile(login.email);

                    if (result.Item1 != login.email) {
                        server.SendToClient(clientId, new LoginResult() {
                            result = Result.Failure,
                            error = "invalid email or password"
                        });
                    } else if (result.Item2 == login.password) {
                        byte[] bitmap = null;
                        if (!String.IsNullOrEmpty(result.Item5)) {
                            try {
                                Log.D(result.Item5);
                                Image img = Image.FromFile(result.Item5);
                                bitmap = ImgToByteArray(img);
                            } catch (Exception e) {
                                Log.E(e.ToString());
                            }
                        }
                        server.SendToClient(clientId, new LoginResult() {
                            result = Result.Success,
                            email = result.Item1,
                            uniqueId = HashString(result.Item1),
                            userName = result.Item3,
                            bio = result.Item4,
                            profile = bitmap
                        });
                        emailtoclientid.Remove(result.Item1);
                        emailtoclientid.Add(result.Item1, clientId);
                    } else {
                        server.SendToClient(clientId, new LoginResult() {
                            result = Result.Failure,
                            error = "invalid password or email"
                        });
                    }
                }
                #endregion
                #region RetrieveUserProfile
                else if (Msg.TryCast(dataType, data, (int)MsgType.RetrieveUserProfile, out RetrieveUserProfile retrieveuserprofile)) {
                    var result = database.GetSelectUserProfile(retrieveuserprofile.email);
                    if (result.Item1 != retrieveuserprofile.email) {
                        server.SendToClient(clientId, new LoginResult() {
                            result = Result.Failure,
                            error = "invalid email or password"
                        });
                    } else {
                        byte[] bitmap = null;
                        if (!String.IsNullOrEmpty(result.Item5)) {
                            try {
                                Image img = Image.FromFile(result.Item5);
                                bitmap = ImgToByteArray(img);
                            } catch (Exception e) {
                                Log.E(e.ToString());
                            }
                        }
                        server.SendToClient(clientId, new LoginResult() {
                            result = Result.Success,
                            email = result.Item1,
                            uniqueId = HashString(result.Item1),
                            userName = result.Item3,
                            bio = result.Item4,
                            profile = bitmap
                        });
                    }
                }
                #endregion
                #region RegisterUser
                else if (Msg.TryCast(dataType, data, (int)MsgType.RegisterUser, out RegisterUser registeruser)) {

                    var result = database.InsertUser(registeruser.email, registeruser.password, registeruser.name, registeruser.bio, "");
                    if (result) {
                        server.SendToClient(clientId, new LoginResult() {
                            result = Result.Success,
                            email = registeruser.email,
                            uniqueId = HashString(registeruser.email),
                            userName = registeruser.name,
                            bio = registeruser.bio
                        });
                    } else {
                        server.SendToClient(clientId, new LoginResult() {
                            result = Result.Failure,
                            error = "insert failed see database log for details",
                            callingType = MsgType.RegisterUser
                        });
                    }
                }
                #endregion
                #region UpdateUserProfile
                else if (Msg.TryCast(dataType, data, (int)MsgType.UpdateUserProfile, out UpdateUserProfile updateuserprofile)) {
                    Log.D("updating profile");
                    String profile = "";
                    if (updateuserprofile.profile != null && updateuserprofile.profile.Length > 0) {
                        profile = directory + updateuserprofile.email.GetHashCode() + ".jpg";
                        using (Image image = Image.FromStream(new MemoryStream(updateuserprofile.profile))) {
                            if (File.Exists(profile)) {
                                File.Delete(profile);
                            }
                            image.Save(profile, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                    }
                    var prior = database.GetSelectUserProfile(updateuserprofile.email);
                    var newName = updateuserprofile.name;
                    var newBio = updateuserprofile.bio;
                    if (String.IsNullOrEmpty(newName)) {
                        newName = prior.Item3;
                    }

                    if (String.IsNullOrEmpty(newBio)) {
                        newBio = prior.Item4;
                    }

                    var result = database.UpdateUser(updateuserprofile.email, prior.Item2, newName, updateuserprofile.bio, profile);

                    if (result) {
                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Success,
                            callingType = MsgType.UpdateUserProfile,
                            type = MsgType.MessageResult
                        });
                    } else {
                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Failure,
                            error = "update failed see database log for details",
                            callingType = MsgType.UpdateUserProfile,
                            type = MsgType.MessageResult
                        });
                    }
                }
                #endregion
                #region UpdateUserPassword
                else if (Msg.TryCast(dataType, data, (int)MsgType.UpdateUserPassword, out UpdateUserPassword updateuserpassword)) {
                    var prior = database.GetSelectUserProfile(updateuserpassword.email);
                    if (updateuserpassword.oldPassword == prior.Item2) {
                        var result = database.UpdateUser(updateuserpassword.email, updateuserpassword.newPassword, prior.Item3, prior.Item4, prior.Item5);
                        if (result) {
                            server.SendToClient(clientId, new MessageResult() {
                                result = Result.Success,
                                callingType = MsgType.UpdateUserPassword,
                                type = MsgType.MessageResult
                            });
                        } else {
                            server.SendToClient(clientId, new MessageResult() {
                                result = Result.Failure,
                                error = "update failed see database log for details",
                                callingType = MsgType.UpdateUserPassword,
                                type = MsgType.MessageResult
                            });
                        }
                    } else {
                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Failure,
                            error = "Incorrect Password",
                            callingType = MsgType.UpdateUserPassword,
                            type = MsgType.MessageResult
                        });
                    }
                }
                #endregion
                #region SendFriendRequest
                else if (Msg.TryCast(dataType, data, (int)MsgType.SendFriendRequest, out SendFriendRequest sendfriendrequest)) {
                    var result = database.InsertFriendRequest(sendfriendrequest.fromEmail, sendfriendrequest.toEmail);
                    if (result) {
                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Success,
                            callingType = MsgType.SendFriendRequest,
                            type = MsgType.MessageResult
                        });
                    } else {
                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.SendFriendRequest,
                            type = MsgType.MessageResult
                        });
                    }
                }

                #endregion
                #region AcceptFriend
                else if (Msg.TryCast(dataType, data, (int)MsgType.AcceptFriend, out AcceptFriend acceptfriend)) {
                    var result = database.InsertFriend(acceptfriend.fromEmail, acceptfriend.toEmail);
                    var id = database.GetConversationsBetween(acceptfriend.fromEmail, acceptfriend.toEmail);
                    if (result) {
                        server.SendToClient(clientId, new AcceptFriendResult() {
                            result = Result.Success,
                            callingType = MsgType.AcceptFriend,
                            conversationId = id
                        });
                    } else {
                        server.SendToClient(clientId, new AcceptFriendResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.AcceptFriend
                        });
                    }
                }

                #endregion GetMyFriendRequests
                #region GetMyFriendRequests
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetMyFriendRequests, out GetMyFriendRequests getmyfriendrequests)) {
                    var result = database.GetUserRequestSent(getmyfriendrequests.email);
                    if (result != null) {
                        var returnemail = new List<String>();
                        var returnname = new List<String>();
                        var returnuserid = new List<String>();
                        for (int i = 0; i < result.Count; i++) {
                            returnemail.Add(result[i].Item1);
                            returnname.Add(result[i].Item3);
                            returnuserid.Add(HashString(result[i].Item1));
                        }
                        server.SendToClient(clientId, new GetMyFriendRequestsResult() {
                            result = Result.Success,
                            callingType = MsgType.GetMyFriendRequests,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            name = returnname.ToArray()

                        });
                    }
                    {
                        server.SendToClient(clientId, new GetMyFriendRequestsResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetMyFriendRequests
                        });
                    }
                }
                #endregion
                #region GetFriendsRequestingMe
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetFriendsRequestingMe, out GetFriendsRequestingMe getfriendsrequestingme)) {
                    var result = database.GetUserRequestRecived(getfriendsrequestingme.email);
                    if (result != null) {
                        var returnemail = new List<String>();
                        var returnname = new List<String>();
                        var returnuserid = new List<String>();
                        for (int i = 0; i < result.Count; i++) {
                            returnemail.Add(result[i].Item1);
                            returnname.Add(result[i].Item3);
                            returnuserid.Add(HashString(result[i].Item1));
                        }
                        server.SendToClient(clientId, new GetFriendsRequestingMeResult() {
                            result = Result.Success,
                            callingType = MsgType.GetFriendsRequestingMe,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            name = returnname.ToArray()

                        });
                    } else {
                        server.SendToClient(clientId, new GetFriendsRequestingMeResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetFriendsRequestingMe
                        });
                    }
                }
                #endregion
                #region GetFriends
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetFriends, out GetFriends getfriends)) {
                    var result = database.GetUserFriends(getfriends.email);
                    if (result != null) {
                        var returnemail = new List<String>();
                        var returnname = new List<String>();
                        var returnuserid = new List<String>();
                        var returnconversationid = new List<String>();
                        for (int i = 0; i < result.Count; i++) {
                            returnemail.Add(result[i].Item1);
                            returnname.Add(result[i].Item2);
                            returnuserid.Add(HashString(result[i].Item1));
                            //this will be removed later and is a temporary way to get conversation ID (this is inefficient and Coversation ID needs to be added to GetFriends query)
                            returnconversationid.Add(database.GetConversationsBetween(getfriends.email, (result[i].Item1)));
                        }
                        server.SendToClient(clientId, new GetFriendsResult() {
                            result = Result.Success,
                            callingType = MsgType.GetFriends,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            name = returnname.ToArray(),
                            conversationId = returnconversationid.ToArray()

                        });
                    } else {
                        server.SendToClient(clientId, new GetFriendsResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetFriends
                        });
                    }
                }
                #endregion
                #region SendMessage
                else if (Msg.TryCast(dataType, data, (int)MsgType.SendMessage, out SendMessage sendmessage)) {
                    var result = database.InsertMessage(sendmessage.conversationId, sendmessage.email, sendmessage.text);
                    if (result) {
                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Success,
                            callingType = MsgType.SendMessage,
                            type = MsgType.MessageResult
                        });
                    } else {
                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.SendMessage,
                            type = MsgType.MessageResult
                        });
                    }
                }
                #endregion
                #region RetrieveMessages
                else if (Msg.TryCast(dataType, data, (int)MsgType.RetrieveMessages, out RetrieveMessages retrievemessages)) {
                    var result = database.GetMessages(retrievemessages.conversationId);
                    if (result != null) {
                        var returnchat = new List<Chat>();
                        var numberorerror = 0;
                        for (int i = 0; i < result.Count; i++) {
                            try {
                                returnchat.Add(new Chat() {
                                    text = result[i].Item1,
                                    dateTime = long.Parse(result[i].Item2),
                                    email = result[i].Item3,
                                    userId = HashString(result[i].Item3)

                                });
                            } catch (Exception e) {
                                numberorerror++;
                            }
                        }
                        server.SendToClient(clientId, new RetrieveMessagesResult() {
                            result = Result.Success,
                            callingType = MsgType.RetrieveMessages,
                            chat = returnchat.ToArray(),
                            error = numberorerror.ToString()

                        });
                    } else {
                        server.SendToClient(clientId, new RetrieveMessagesResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.RetrieveMessages
                        });
                    }
                }
                #endregion
                #region GetMatchDetails
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetMatchDetails, out GetMatchDetails getmatchdetails)) {
                    var result = database.GetMatch(getmatchdetails.matchId);
                    if (result.Item1 != getmatchdetails.matchId) {
                        server.SendToClient(clientId, new GetMatchDetailsResult() {
                            result = Result.Failure,
                            error = "query match id did not match requested match id (possibly zero results)"
                        });
                    } else {
                        server.SendToClient(clientId, new GetMatchDetailsResult() {
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
                #region CreateMatch 
                else if (Msg.TryCast(dataType, data, (int)MsgType.CreateMatch, out CreateMatch creatematch)) {
                    if (emailtoclientid.ContainsKey(creatematch.email_1) && emailtoclientid.ContainsKey(creatematch.email_2)) {
                        var result = database.InsertMatch(creatematch.email_1, creatematch.email_2);
                        //get conversation ID or create if non exist
                        var coversation = database.GetConversationsBetween(creatematch.email_1, creatematch.email_2);
                        if (coversation == "invalid") {
                            database.InsertConversation(creatematch.email_1, creatematch.email_2);
                            coversation = database.GetConversationsBetween(creatematch.email_1, creatematch.email_2);
                        }
                        if (!String.IsNullOrEmpty(result)) {


                            server.SendToClient(emailtoclientid[creatematch.email_1], new CreateMatchResult() {
                                result = Result.Success,
                                callingType = MsgType.CreateMatch,
                                matchId = result,
                                conversationId = coversation
                            });
                            server.SendToClient(emailtoclientid[creatematch.email_2], new CreateMatchResult() {
                                result = Result.Success,
                                callingType = MsgType.CreateMatch,
                                matchId = result,
                                conversationId = coversation
                            });
                        } else {
                            server.SendToClient(clientId, new CreateMatchResult() {
                                result = Result.Failure,
                                error = "match request failed see database log for details",
                                callingType = MsgType.CreateMatch
                            });
                        }
                    } else {
                        server.SendToClient(clientId, new CreateMatchResult() {
                            result = Result.Failure,
                            callingType = MsgType.CreateMatch,
                            error = "failed to find clientID for 1 or both users"
                        });
                    }


                }
                #endregion 
                #region GetMatchHistory
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetMatchHistory, out GetMatchHistory getmatchhistory)) {
                    var result = database.GetUserMatchHistory(getmatchhistory.email_1);
                    if (result != null) {
                        var returnemail = new List<String>();
                        var returnuserid = new List<String>();
                        var returnmatchid = new List<String>();
                        var returnboard = new List<String>();
                        var returnended = new List<bool>();
                        var returnstartdate = new List<Int64>();
                        for (int i = 0; i < result.Count; i++) {
                            returnemail.Add(result[i].Item3);
                            returnuserid.Add(HashString(result[i].Item3));
                            returnmatchid.Add(result[i].Item2);
                            returnstartdate.Add(long.Parse(result[i].Item1));
                            returnboard.Add(result[i].Item5);
                        }
                        server.SendToClient(clientId, new GetMatchHistoryResult() {
                            result = Result.Success,
                            callingType = MsgType.GetMatchHistory,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            board = returnboard.ToArray(),
                            startDateTime = returnstartdate.ToArray(),
                            ended = returnended.ToArray()

                        });
                    } else {
                        server.SendToClient(clientId, new GetFriendsResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetFriends
                        });
                    }
                }
                #endregion
                #region EnterMatchQueue
                else if (Msg.TryCast(dataType, data, (int)MsgType.EnterMatchQueue, out EnterMatchQueue entermatchqueue)) {

                    if (playqueue.Count >= 1 && !playqueue.Contains(entermatchqueue.email)) {
                        var opponent = playqueue[0];
                        playqueue.Remove(opponent);
                        var result = database.InsertMatch(entermatchqueue.email, opponent);
                        //get conversation ID or create if non exist
                        var coversation = database.GetConversationsBetween(entermatchqueue.email, opponent);
                        {
                            database.InsertConversation(creatematch.email_1, creatematch.email_2);
                            coversation = database.GetConversationsBetween(creatematch.email_1, creatematch.email_2);
                        }
                        if (!String.IsNullOrEmpty(result)) {
                            server.SendToClient(clientId, new MatchFound() {
                                result = Result.Success,
                                callingType = MsgType.EnterMatchQueue,
                                matchId = result,
                                conversationId = coversation,
                                opponentemail = opponent,
                                youremail = entermatchqueue.email
                            });
                            emailtoclientid.TryGetValue(opponent, out string opponentid);
                            server.SendToClient(opponentid, new MatchFound() {
                                result = Result.Success,
                                callingType = MsgType.EnterMatchQueue,
                                matchId = result,
                                conversationId = coversation,
                                opponentemail = entermatchqueue.email,
                                youremail = opponent
                            });
                        } else {
                            server.SendToClient(clientId, new MatchFound() {
                                result = Result.Failure,
                                error = "something went wrong during matchmaking",
                                callingType = MsgType.EnterMatchQueue,
                                type = MsgType.MatchFound
                            });
                        }
                    } else {
                        if (!playqueue.Contains(entermatchqueue.email)) {
                            playqueue.Add(entermatchqueue.email);
                        }


                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Success,
                            error = "you are in the queue",
                            callingType = MsgType.EnterMatchQueue
                        });

                    }
                }
                #endregion
                #region ExitMatchQueue
                else if (Msg.TryCast(dataType, data, (int)MsgType.ExitMatchQueue, out ExitMatchQueue exitmatchqueue)) {
                    playqueue.Remove(exitmatchqueue.email);
                }

                #endregion
                #region SendChallenge
                else if (Msg.TryCast(dataType, data, (int)MsgType.SendChallenge, out SendChallenge sendchallenge)) {
                    if (emailtoclientid.ContainsKey(sendchallenge.opponentemail)) {

                        server.SendToClient(emailtoclientid[sendchallenge.opponentemail], new SendChallenge() {
                            youremail = sendchallenge.opponentemail,
                            opponentemail = sendchallenge.youremail,
                            type = MsgType.SendChallenge
                        });

                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Success,
                            error = "opponent not logged in",
                            callingType = MsgType.SendChallenge,
                            type = MsgType.MessageResult
                        });
                    } else {
                        server.SendToClient(clientId, new MessageResult() {
                            result = Result.Failure,
                            error = "opponent not logged in",
                            callingType = MsgType.SendChallenge,
                            type = MsgType.MessageResult
                        });
                    }

                }
                #endregion
                #region AcceptChallenge
                else if (Msg.TryCast(dataType, data, (int)MsgType.AcceptChallenge, out AcceptChallenge acceptchallenge)) {
                    if (acceptchallenge.Accept) {
                        if (emailtoclientid.ContainsKey(acceptchallenge.opponentemail) && emailtoclientid.ContainsKey(acceptchallenge.youremail)) {
                            var result = database.InsertMatch(acceptchallenge.opponentemail, acceptchallenge.youremail);
                            //get conversation ID or create if non exist
                            var coversation = database.GetConversationsBetween(acceptchallenge.opponentemail, acceptchallenge.youremail);
                            if (coversation == "invalid") {
                                database.InsertConversation(acceptchallenge.opponentemail, acceptchallenge.youremail);
                                coversation = database.GetConversationsBetween(acceptchallenge.opponentemail, acceptchallenge.youremail);
                            }
                            if (!String.IsNullOrEmpty(result)) {


                                server.SendToClient(emailtoclientid[acceptchallenge.opponentemail], new CreateMatchResult() {
                                    result = Result.Success,
                                    callingType = MsgType.CreateMatch,
                                    matchId = result,
                                    conversationId = coversation
                                });
                                server.SendToClient(emailtoclientid[acceptchallenge.youremail], new CreateMatchResult() {
                                    result = Result.Success,
                                    callingType = MsgType.CreateMatch,
                                    matchId = result,
                                    conversationId = coversation
                                });
                            } else {
                                server.SendToClient(clientId, new CreateMatchResult() {
                                    result = Result.Failure,
                                    error = "match request failed see database log for details",
                                    callingType = MsgType.CreateMatch
                                });
                            }
                        } else {
                            server.SendToClient(clientId, new CreateMatchResult() {
                                result = Result.Failure,
                                callingType = MsgType.CreateMatch,
                                error = "failed to find clientID for 1 or both users"
                            });
                        }
                    }
                }
                #endregion
                #region UpdateBoard
                else if (Msg.TryCast(dataType, data, (int)MsgType.UpdateBoard, out UpdateBoard updateboard)) {
                    var board = updateboard.board.Split(',').Select(Int32.Parse).ToList();
                    var Match = database.GetMatch(updateboard.matchId);
                    var oldboardstring = Match.Item4;
                    var oldboard = oldboardstring.Split(',').Select(Int32.Parse).ToList();
                    var newboard = "";
                    if (board.Count == 64) {
                        if (updateboard.White) {
                            for (int i = 0; i < 64; i++) {
                                if ((board[i] < 7 && board[i] > 0) || (oldboard[i] < 7 && oldboard[i] > 0)) {
                                    newboard += board[i].ToString();
                                } else {
                                    newboard += oldboard[i].ToString();
                                }
                                if (i < 63) {
                                    newboard += ",";
                                }
                            }
                        } else {
                            for (int i = 0; i < 64; i++) {
                                if ((board[i] > 6) || (oldboard[i] > 6)) {
                                    newboard += board[i].ToString();
                                } else {
                                    newboard += oldboard[i].ToString();
                                }
                                if (i < 63) {
                                    newboard += ",";
                                }
                            }
                        }
                        database.UpdateMatch(updateboard.matchId, Match.Item3, newboard);
                        server.SendToClient(clientId, new BoardResult() {
                            result = Result.Success,
                            board = newboard,
                            matchId = updateboard.matchId
                        });
                    } else {
                        server.SendToClient(clientId, new BoardResult() {
                            result = Result.Invalid,
                            error = "invalid board state Recived"
                        });
                    }
                }
                #endregion
                #region GetBoardState
                else if (Msg.TryCast(dataType, data, (int)MsgType.GetBoardState, out GetBoardState getboardstate)) {
                    var Match = database.GetMatch(getboardstate.matchId);
                    var boardstring = Match.Item4;
                    server.SendToClient(clientId, new BoardResult() {
                        result = Result.Success,
                        board = boardstring,
                        matchId = getboardstate.matchId
                    });
                }
                #endregion
                #region RetrieveOtherUsers
                else if (Msg.TryCast(dataType, data, (int)MsgType.RetrieveOtherUsers, out RetrieveOtherUsers retrieveotherusers)) {
                    var result = database.GetAllOtherUserProfile(retrieveotherusers.email, retrieveotherusers.search, retrieveotherusers.limit, retrieveotherusers.offset);
                    if (result != null) {
                        var returnemail = new List<String>();
                        var returnname = new List<String>();
                        var returnuserid = new List<String>();
                        var returnbio = new List<String>();
                        for (int i = 0; i < result.Count; i++) {
                            returnemail.Add(result[i].Item1);
                            returnname.Add(result[i].Item2);
                            returnuserid.Add(HashString(result[i].Item1));
                            returnbio.Add(result[i].Item3);
                        }
                        server.SendToClient(clientId, new RetrieveOtherUsersResult() {
                            result = Result.Success,
                            callingType = MsgType.RetrieveOtherUsers,
                            email = returnemail.ToArray(),
                            userId = returnuserid.ToArray(),
                            name = returnname.ToArray(),
                            bio = returnbio.ToArray()

                        });
                    } else {
                        server.SendToClient(clientId, new GetFriendsResult() {
                            result = Result.Failure,
                            error = "friend request failed see database log for details",
                            callingType = MsgType.GetFriends
                        });
                    }

                }

                #endregion
                #region Invalid
                else {
                    server.SendToClient(clientId, new MessageResult() {
                        result = Result.Invalid,
                        error = "error or listener not implamanted ",
                        callingType = MsgType.SendFriendRequest,
                        type = MsgType.MessageResult
                    });
                }
                #endregion
            };

            server.Begin();

            Console.ReadKey();
            server.End();
        }

        //hash for user id
        public static string HashString(string text) {
            const string chars = "0123456789abcdefghijklmnopqrztuvABCDEFGHIJKLMNOPQRSTUVWXYZ";
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);

            char[] hash2 = new char[32];

            // Note that here we are wasting bits of hash! 
            // But it isn't really important, because hash.Length == 32
            for (int i = 0; i < hash2.Length; i++) {
                hash2[i] = chars[hash[i] % chars.Length];
            }

            return new string(hash2);
        }

        //convert image to bytearray
        public static byte[] ImgToByteArray(Image img) {
            using (MemoryStream mStream = new MemoryStream()) {
                img.Save(mStream, img.RawFormat);
                return mStream.ToArray();
            }
        }

    }
}