using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Include.Util;
using MySql.Data;
using MySql.Data.MySqlClient;

/// <summary>
///Author Mark Bird 43743050 for DECO7381
/// </summary>
namespace DatabaseConnection
{
    public class Database
    {
        private DBConnection dbCon;
        private string dbName = "deco7381_build";

        public Database()
        {
            dbCon = DBConnection.Instance();
            dbCon.DatabaseName = dbName;
        }


        //----------------------section for select SQL statements-------------------------------------
        /// <summary>
        /// retrives a users profile from the database
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns>a tupple with Email, Password, Name, Bio, profile(file location) all as strings</returns>
        public Tuple<string, string, string, string, string> GetSelectUserProfile(string UserEmail)
        {
            string[] results = { "", "", "", "", "" };
            if (dbCon.IsConnect())
            {
                string query = "SELECT Email, Password, Name, Bio, Profile_Pic FROM users WHERE Email  = @UserEmail";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (!reader.IsDBNull(i))
                        {
                            results[i] = reader.GetString(i);
                        }
                    }
                    Log.D("Database: Email:" + results[0] + ", Password:" + results[1] + ", Name:" + results[2] + ", Bio:" + results[3] + ", Profile:" + results[4]);
                }
                reader.Close();
            }
            return Tuple.Create(results[0], results[1], results[2], results[3], results[4]);
        }

        /// <summary>
        /// retrives all information about a match between 2 players based on a match ID
        /// </summary>
        /// <param name="Match_ID"></param>
        /// <returns></returns>
        public Tuple<string, string, string, string, string, string> GetMatch(string Match_ID)
        {
            string[] results = { "", "", "", "", "", "" };
            if (dbCon.IsConnect())
            {
                string query = "SELECT matches.Match_ID, Start_DateTime, Ended, Last_Board_State, U1.Email, U2.Email FROM matches, match_between AS U1, match_between AS U2 WHERE matches.Match_ID  = " + Match_ID + " AND matches.Match_ID = U1.Match_ID AND U2.Match_ID = matches.Match_ID AND U1.Email > U2.Email";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (!reader.IsDBNull(i))
                        {
                            results[i] = reader.GetString(i);
                        }
                    }
                    Log.D("Database: Start:" + results[1] + ", Ended:" + results[2] + ", Last Board State:" + results[3] + ", Match ID:" + results[0] + ", User 1:" + results[4] + ", User 2:" + results[5]);
                }
                reader.Close();
            }
            return Tuple.Create(results[0], results[1], results[2], results[3], results[4], results[5]);
        }

        /// <summary>
        /// get all user profiles except for 1 user
        /// used for searching for friends and such
        /// should set limit and offset later
        /// should remove password but not worth the effort 
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns>a list of tupples containing Email, Password, Name, Bio, profile(file location) all as strings</returns>
        public List<Tuple<string, string, string, string>> GetAllOtherUserProfile(string UserEmail)
        {
            string[] results = { "", "", "", "", "" };
            var myReturn = new List<Tuple<string, string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Email, Name, Bio, Profile_Pic FROM users WHERE Email  != @UserEmail";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (!reader.IsDBNull(i))
                        {
                            results[i] = reader.GetString(i);
                        }
                    }
                    Log.D("Database: Email:" + results[0] +", Name:" + results[1] + ", Bio:" + results[2] + ", Profile:" + results[3]);
                    myReturn.Add(Tuple.Create(results[0], results[1], results[2], results[3]));
                }
                reader.Close();
            }
            return myReturn;
        }

        /// <summary>
        /// get a list of a users friends emails and names
        /// TODO add Conversation ID's
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns>a list of tupples containing Email and name</returns>
        public List<Tuple<string, string>> GetUserFriends(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Email_1, Name FROM friends, users WHERE Email_2  = @UserEmail AND users.Email = friends.Email_1";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
                }

                reader.Close();
                string query2 = "SELECT Email_2, Name FROM friends, users WHERE Email_1  = @UserEmail AND users.Email = friends.Email_2";
                var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                cmd2.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd2.Prepare();
                var reader2 = cmd2.ExecuteReader();
                while (reader2.Read())
                {
                    myReturn.Add(Tuple.Create(reader2.GetString(0), reader2.GetString(1)));
                }

                reader2.Close();
            }
            return myReturn;
        }

        /// <summary>
        /// get a list of Friends Requests sent by user
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns>a list of tuples containing Email, DateTime and Name as strings</returns>
        public List<Tuple<string, string, string>> GetUserRequestSent(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Reciver, DateTime, Name FROM friend_request, users WHERE Sender  = @UserEmail AND users.Email = friend_request.Reciver";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                }

                reader.Close();
            }
            return myReturn;
        }

        /// <summary>
        /// get a list of friend requests recived
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns>a list of tuples containing Email, DateTime and Name as strings</returns>
        public List<Tuple<string, string, String>> GetUserRequestRecived(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string, String>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Sender, DateTime, Name FROM friend_request, users WHERE Reciver  = @UserEmail AND users.Email = friend_request.Sender";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                }

                reader.Close();
            }
            return myReturn;
        }

        /// <summary>
        /// NOT IN USE
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns></returns>
        public List<Tuple<string, string>> GetUserConversations(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Display_Name, Conversation_ID FROM in_conversation WHERE Email  = @UserEmail";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
                }

                reader.Close();
            }
            return myReturn;
        }
        /// <summary>
        /// NOT IN USE
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns></returns>
        public List<Tuple<string, string, string, string>> GetUserConversationsFull(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT  in_conversation.Display_Name, in_conversation.Conversation_ID, users.Email, users.Name FROM in_conversation, users WHERE Conversation_ID IN(SELECT Conversation_ID FROM in_conversation WHERE in_conversation.Email = @UserEmail) AND users.Email != @UserEmail AND users.Email = in_conversation.Email";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
                }

                reader.Close();
            }
            return myReturn;
        }

        /// <summary>
        /// A method to retrice the Conversation ID of a conversation between 2 users
        /// </summary>
        /// <param name="Email_1"></param>
        /// <param name="Email_2"></param>
        /// <returns>the conversation ID as a string</returns>
        public string GetConversationsBetween(string Email_1, string Email_2)
        {
            var myReturn = "invalid";
            if (dbCon.IsConnect())
            {
                string query = "SELECT Conversation_ID FROM in_conversation WHERE Email = @UserEmail_1 AND Conversation_ID IN (SELECT Conversation_ID FROM in_conversation WHERE Email = @UserEmail_2) LIMIT 1";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail_1", Email_1);
                cmd.Parameters.AddWithValue("@UserEmail_2", Email_2);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn = reader.GetString(0);
                }

                reader.Close();
            }
            return myReturn;
        }

        /// <summary>
        /// retrive a users match history
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns></returns>
        public List<Tuple<string, string, string, string, string>> GetUserMatchHistory(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string, string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT matches.Start_DateTime, match_between.Match_ID, users.Email, users.Name, matches.Last_Board_State FROM match_between, users, matches WHERE match_between.Match_ID IN(SELECT Match_ID FROM match_between WHERE Email = @UserEmail) AND(users.Email != @UserEmail) AND (users.Email = match_between.Email) AND (matches.Match_ID=match_between.Match_ID) ORDER BY matches.Start_DateTime DESC";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                cmd.Parameters.AddWithValue("@UserEmail", UserEmail);
                cmd.Prepare();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
                }

                reader.Close();
            }
            return myReturn;
        }

        /// <summary>
        /// get all users open matches
        /// NOT IN USE
        /// </summary>
        /// <param name="UserEmail"></param>
        /// <returns></returns>
        public List<Tuple<string, string, string, string>> GetUserOpenMatch(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT matches.Start_DateTime, match_between.Match_ID, users.Email, users.Name FROM match_between, users, matches WHERE match_between.Match_ID IN(SELECT Match_ID FROM match_between WHERE Email = @UserEmail) AND(users.Email != @UserEmail) AND (users.Email = match_between.Email) AND (matches.Match_ID=match_between.Match_ID) AND (matches.Ended !=1) ORDER BY matches.Start_DateTime DESC";
                Log.D(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
                }

                reader.Close();
            }
            return myReturn;
        }




        /// <summary>
        /// returns all messages in a conversation in reverse cronological order (most recent to olders)
        /// </summary>
        /// <param name="Conversation_ID"></param>
        /// <returns>a list of tuples containing Text, DateTime, Sender Email as strings</returns>
        public List<Tuple<string, string, string>> GetMessages(string Conversation_ID)
        {
            var myReturn = new List<Tuple<string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Text, DateTime, Sender_Email FROM message WHERE Conversation_ID  = " + Conversation_ID + " ORDER BY DateTime DESC";
                Log.D(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1), reader.GetString(1)));
                }

                reader.Close();
            }
            return myReturn;
        }


        //--------------------------Section for Update SQL statements-------------------------------
        /// <summary>
        /// used to update a users profile
        /// not cannot update Email using this method
        /// </summary>
        /// <param name="Email"></param>
        /// <param name="Password"></param>
        /// <param name="Name"></param>
        /// <param name="Bio"></param>
        /// <param name="Profile"></param>
        /// <returns>true if the update succeded</returns>
        public Boolean UpdateUser(string Email, string Password, string Name, string Bio, string Profile)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "Update users SET Password = @Password, Name = @Name, Bio = @Bio, Profile_Pic = @Profile WHERE Email = @Email";
                    Log.D(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Parameters.AddWithValue("@Password", Password);
                    cmd.Parameters.AddWithValue("@Name", Name);
                    cmd.Parameters.AddWithValue("@Bio", Bio);
                    cmd.Parameters.AddWithValue("@Profile", Profile);
                    cmd.Prepare();
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// update a match to indicate it has finished or that the board state has changed
        /// </summary>
        /// <param name="Match_ID"></param>
        /// <param name="Ended"></param>
        /// <param name="Board_State"></param>
        /// <returns></returns>
        public Boolean UpdateMatch(string Match_ID, string Ended, string Board_State)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "Update matches SET Last_Board_State = " + Board_State + ", Ended = " + Ended + " WHERE Match_ID = '" + Match_ID + "'";
                    Log.D(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// NOT IN USE
        /// </summary>
        /// <param name="Conversation_ID"></param>
        /// <param name="NewName"></param>
        /// <param name="Email"></param>
        /// <returns></returns>
        public Boolean UpdateConversationName(string Conversation_ID, string NewName, string Email)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "Update in_conversation SET Display_Name = @NewName WHERE Conversation_ID = " + Conversation_ID + " AND Email = @Email";
                    Log.D(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    cmd.Parameters.AddWithValue("@NewName", NewName);
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Prepare();
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        //--------------------------Section for Remove SQL statements-------------------------------
        /// <summary>
        /// used to remove a user from their friend list
        /// Order not Important
        /// NOT I USE
        /// </summary>
        /// <param name="Email_1"></param>
        /// <param name="Email_2"></param>
        /// <returns></returns>
        public Boolean RemoveFriend(string Email_1, string Email_2)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "DELETE FROM friends WHERE (Email_1 = @Email_1 AND Email_2 = @Email_2) OR (Email_1 = @Email_2 AND Email_2 = @Email_1) ";
                    Log.D(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    cmd.Parameters.AddWithValue("@Email_1", Email_1);
                    cmd.Parameters.AddWithValue("@Email_2", Email_2);
                    cmd.Prepare();
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }




        //--------------------------Section for insert SQL statements-------------------------------

        /// <summary>
        /// Insert a new user into the database
        /// </summary>
        /// <param name="Email"></param>
        /// <param name="Password"></param>
        /// <param name="Name"></param>
        /// <param name="Bio"></param>
        /// <param name="Profile"></param>
        /// <returns></returns>
        public Boolean InsertUser(string Email, string Password, string Name, string Bio, string Profile)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "INSERT INTO users (Email, Password, Name, Bio, Profile_Pic) VALUES (@Email, @Password, @Name, @Bio, @Profile)";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Parameters.AddWithValue("@Password", Password);
                    cmd.Parameters.AddWithValue("@Name", Name);
                    cmd.Parameters.AddWithValue("@Bio", Bio);
                    cmd.Parameters.AddWithValue("@Profile", Profile);
                    cmd.Prepare();
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public Boolean InsertFriendRequest(string FromEmail, string ToEmail)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "INSERT INTO friend_request (Sender, Reciver, DateTime) VALUES (@FromEmail, @ToEmail," + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ")";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    cmd.Parameters.AddWithValue("@FromEmail", FromEmail);
                    cmd.Parameters.AddWithValue("@ToEmail", ToEmail);
                    cmd.Prepare();
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public Boolean InsertFriend(string RequestFromEmail, string AcceptedEmail)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    //remove from friend request
                    string query = "DELETE FROM friend_request WHERE (Sender = @RequestFromEmail AND  Reciver = @AcceptedEmail) OR (Sender = @AcceptedEmail AND  Reciver = @RequestFromEmail)";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    cmd.Parameters.AddWithValue("@RequestFromEmail", RequestFromEmail);
                    cmd.Parameters.AddWithValue("@AcceptedEmail", AcceptedEmail);
                    cmd.Prepare();
                    var result = cmd.ExecuteNonQuery();
                    Log.D(result.ToString());

                    string query2 = "INSERT INTO friends (Email_1, Email_2) VALUES (@RequestFromEmail, @AcceptedEmail)";
                    Log.D(query2);
                    var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                    cmd2.Parameters.AddWithValue("@RequestFromEmail", RequestFromEmail);
                    cmd2.Parameters.AddWithValue("@AcceptedEmail", AcceptedEmail);
                    cmd2.Prepare();
                    var result2 = cmd2.ExecuteNonQuery();
                    if (result2 == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public Boolean InsertConversation(string Email_1, string Email_2)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    //create the conversation
                    string query = "INSERT INTO conversation(`Conversation_ID`) VALUES  (NULL)";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    Log.D(result.ToString());

                    //add users to the conversation just created
                    string query2 = "INSERT INTO in_conversation (Display_Name, Conversation_ID, Email) VALUES ('unName',LAST_INSERT_ID(), @Email_2), ('unName',LAST_INSERT_ID(),@Email_1)";
                    Log.D(query2);
                    var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                    cmd2.Parameters.AddWithValue("@Email_1", Email_1);
                    cmd2.Parameters.AddWithValue("@Email_2", Email_2);
                    cmd2.Prepare();
                    var result2 = cmd2.ExecuteNonQuery();
                    if (result + result2 == 3) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public Boolean InsertMessage(string Conversation_ID, string Email_Of_Sender, string text)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    //insert the message
                    string query = "INSERT INTO message (Text,Conversation_ID,Sender_Email,DateTime) VALUES  (@text," + Conversation_ID + ", @Email_Of_Sender," + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ")";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    cmd.Parameters.AddWithValue("@Email_Of_Sender", Email_Of_Sender);
                    cmd.Parameters.AddWithValue("@text", text);
                    cmd.Prepare();
                    var result = cmd.ExecuteNonQuery();
                    Log.D(result.ToString());
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public String InsertMatch(string Email_1, string Email_2)
        {
            var myReturn = "";
            if (dbCon.IsConnect())
            {
                try
                {
                    //create the match
                    string query = "INSERT INTO matches (`Match_ID`) VALUES (NULL)";
                    Log.D(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    //get match ID
                    var query3 = "SELECT LAST_INSERT_ID() FROM matches";
                    Log.D(query3);
                    var cmd3 = new MySqlCommand(query3, dbCon.Connection);
                    var reader = cmd3.ExecuteReader();

                    while (reader.Read())
                    {
                        myReturn = reader.GetString(0);
                    }

                    reader.Close();

                    //add users to the match just created
                    string query2 = "INSERT INTO match_between (Match_ID, Email) VALUES (LAST_INSERT_ID(),@Email_1), (LAST_INSERT_ID(), @Email_2)";
                    Log.D(query2);
                    var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                    cmd2.Parameters.AddWithValue("@Email_1", Email_1);
                    cmd2.Parameters.AddWithValue("@Email_2", Email_2);
                    cmd2.Prepare();
                    var result2 = cmd2.ExecuteNonQuery();
                    if (result + result2 == 3) { return myReturn; }
                    else { return ""; }
                }
                catch (Exception e)
                {
                    Log.D("Database: shit went wrong");
                    Log.E(e.ToString());
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        public void CloseDbCon()
        {
            dbCon.Close();
        }




    }
}
