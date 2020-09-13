using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public Database() {
            dbCon = DBConnection.Instance();
            dbCon.DatabaseName = dbName;
        }
        

        //----------------------section for select SQL statements-------------------------------------
        public Tuple<string, string, string, string, string> GetSelectUserProfile(string UserEmail) {
            string[] results = { "", "", "", "", "" };
            if (dbCon.IsConnect())
            { 
                string query = "SELECT * FROM users WHERE Email  = '"+ UserEmail +"'";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    for (int i = 0; i < 5; i++) {
                        if (!reader.IsDBNull(i))
                        {
                            results[i] = reader.GetString(i);
                        }
                    }
                    Console.WriteLine("Email:" + results[0] + ", Password:" + results[1] + ", Name:" + results[2] + ", Bio:" + results[3] + ", Profile:" + results[4]);
                }
                reader.Close();
            }
            return Tuple.Create(results[0], results[1], results[2], results[3], results[4]);
        }

        public Tuple<string, string, string> GetMatch(string Match_ID)
        {
            string[] results = { "", "", ""};
            if (dbCon.IsConnect())
            {
                string query = "SELECT * FROM matches WHERE Match_ID  = '" + Match_ID + "'";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (!reader.IsDBNull(i+1))
                        {
                            results[i] = reader.GetString(i+1);
                        }
                    }
                    Console.WriteLine("Start:" + results[0] + ", Ended:" + results[1] + ", Last Board State:" + results[2]);
                }
                reader.Close();
            }
            return Tuple.Create(results[0], results[1], results[2]);
        }

        public List<Tuple<string, string, string, string, string>> GetAllOtherUserProfile(string UserEmail)
        {
            string[] results = { "", "", "", "", "" };
            var myReturn = new List<Tuple<string, string, string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT * FROM users WHERE Email  != '" + UserEmail + "'";
                Console.WriteLine(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
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
                    Console.WriteLine("Email:" + results[0] + ", Password:" + results[1] + ", Name:" + results[2] + ", Bio:" + results[3] + ", Profile:" + results[4]);
                    myReturn.Add(Tuple.Create(results[0], results[1], results[2], results[3], results[4]));
                }
                reader.Close();
            }
            return myReturn;
        }

        public List<Tuple<string>> GetUserFriends(string UserEmail)
        {
            var myReturn = new List<Tuple<string>>();
            if (dbCon.IsConnect())
            {                
                string query = "SELECT Email_1 FROM friends WHERE Email_2  = '" + UserEmail + "'";
                Console.WriteLine(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0)));
                }

                reader.Close();
                string query2 = "SELECT Email_2 FROM friends WHERE Email_1  = '" + UserEmail + "'";
                Console.WriteLine(query2);
                var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                var reader2 = cmd2.ExecuteReader();
                while (reader2.Read())
                {
                    myReturn.Add(Tuple.Create(reader2.GetString(0)));
                }

                reader2.Close();
            }
            return myReturn;
        }

        public List<Tuple<string,string>> GetUserRequestSent(string UserEmail)
        {
            var myReturn = new List<Tuple<string,string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Reciver, DateTime FROM friend_request WHERE Sender  = '" + UserEmail + "'";
                Console.WriteLine(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
                }

                reader.Close();
            }
            return myReturn;
        }

        public List<Tuple<string, string>> GetUserRequestRecived(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Sender, DateTime FROM friend_request WHERE Reciver  = '" + UserEmail + "'";
                Console.WriteLine(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
                }

                reader.Close();
            }
            return myReturn;
        }

        public List<Tuple<string, string>> GetUserConversations(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Display_Name, Conversation_ID FROM in_conversation WHERE Email  = '" + UserEmail + "'";
                Console.WriteLine(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
                }

                reader.Close();
            }
            return myReturn;
        }

        public List<Tuple<string, string, string, string>> GetUserConversationsFull(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT  in_conversation.Display_Name, in_conversation.Conversation_ID, users.Email, users.Name FROM in_conversation, users WHERE Conversation_ID IN(SELECT Conversation_ID FROM in_conversation WHERE in_conversation.Email = '"+ UserEmail + "') AND users.Email != '" + UserEmail + "' AND users.Email = in_conversation.Email";
                Console.WriteLine(query);
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

        public List<Tuple<string>> GetConversationsBetween(string Email_1, string Email_2)
        {
            var myReturn = new List<Tuple<string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Conversation_ID FROM in_conversation WHERE Email = '"+Email_1+ "' AND Conversation_ID IN (SELECT Conversation_ID FROM in_conversation WHERE Email = '" + Email_2 + "')";
                Console.WriteLine(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0)));
                }

                reader.Close();
            }
            return myReturn;
        }

        public List<Tuple<string,string, string, string>> GetUserMatchHistory(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT matches.Start_DateTime, match_between.Match_ID, users.Email, users.Name FROM match_between, users, matches WHERE match_between.Match_ID IN(SELECT Match_ID FROM match_between WHERE Email = '" + UserEmail + "') AND(users.Email != '" + UserEmail + "') AND (users.Email = match_between.Email) AND (matches.Match_ID=match_between.Match_ID) ORDER BY matches.Start_DateTime DESC";
                Console.WriteLine(query);
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

        public List<Tuple<string, string, string, string>> GetUserOpenMatch(string UserEmail)
        {
            var myReturn = new List<Tuple<string, string, string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT matches.Start_DateTime, match_between.Match_ID, users.Email, users.Name FROM match_between, users, matches WHERE match_between.Match_ID IN(SELECT Match_ID FROM match_between WHERE Email = '" + UserEmail + "') AND(users.Email != '" + UserEmail + "') AND (users.Email = match_between.Email) AND (matches.Match_ID=match_between.Match_ID) AND (matches.Ended !=1) ORDER BY matches.Start_DateTime DESC";
                Console.WriteLine(query);
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



        //returns all messages in a conversation in reverse cronological order (most recent to olders)
        public List<Tuple<string, string>> GetMessages(string Conversation_ID)
        {
            var myReturn = new List<Tuple<string, string>>();
            if (dbCon.IsConnect())
            {
                string query = "SELECT Text, DateTime, Sender_Email FROM message WHERE Conversation_ID  = " + Conversation_ID+ " ORDER BY DateTime DESC";
                Console.WriteLine(query);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    myReturn.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
                }

                reader.Close();
            }
            return myReturn;
        }


        //--------------------------Section for Update SQL statements-------------------------------
        public Boolean UpdateUser(string Email, string Password, string Name, string Bio, string Profile)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "Update users SET Password = '" + Password + "', Name = '" + Name + "', Bio = '" + Bio + "', Profile_Pic =  '" + Profile + "' WHERE Email = '"+ Email + "'";
                    Console.WriteLine(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public Boolean UpdateMatch(string Match_ID, string Ended, string Board_State)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "Update matches SET Last_Board_State = " + Board_State + ", Ended = " + Ended + " WHERE Match_ID = '" + Match_ID + "'";
                    Console.WriteLine(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public Boolean UpdateConversationName(string Conversation_ID, string NewName, string Email)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "Update in_conversation SET Display_Name = '" + NewName + "' WHERE Conversation_ID = " + Conversation_ID + " AND Email = '"+ Email + "'";
                    Console.WriteLine(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        //--------------------------Section for Remove SQL statements-------------------------------
        public Boolean RemoveFriend(string Email_1, string Email_2)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "DELETE FROM friends WHERE (Email_1 = '" + Email_1 + "' AND Email_2 = '" + Email_2 + "') OR(Email_1 = '" + Email_2 + "' AND Email_2 = '" + Email_1 + "') ";
                    Console.WriteLine(query);
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }




        //--------------------------Section for insert SQL statements-------------------------------
        public Boolean InsertUser(string Email, string Password, string Name, string Bio, string Profile) {
            if (dbCon.IsConnect())
            {
                try
                {
                    string query = "INSERT INTO users (Email, Password, Name, Bio, Profile_Pic) VALUES ('" + Email + "','" + Password + "','" + Name + "','" + Bio + "','" + Profile + "')";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e) {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
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
                    string query = "INSERT INTO friend_request (Sender, Reciver) VALUES ('" + FromEmail + "','" + ToEmail + "')";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
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
                    string query = "DELETE FROM friend_request WHERE Sender = '" + RequestFromEmail + "' AND  Reciver ='" + AcceptedEmail + "'";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    Console.WriteLine(result);

                    string query2 = "INSERT INTO friends (Email_1, Email_2) VALUES ('" + RequestFromEmail + "','" + AcceptedEmail + "')";
                    Console.WriteLine(query2);
                    var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                    var result2 = cmd2.ExecuteNonQuery();
                    if (result2 == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
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
                    Console.WriteLine(result); 

                    //add users to the conversation just created
                    string query2 = "INSERT INTO in_conversation (Display_Name, Conversation_ID, Email) VALUES ('test_Name',LAST_INSERT_ID(),'" + Email_2 + "'), ('test_Name',LAST_INSERT_ID(),'" + Email_1 + "')";
                    Console.WriteLine(query2);
                    var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                    var result2 = cmd2.ExecuteNonQuery();
                    if (result + result2 == 3) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
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
                    string query = "INSERT INTO message (Text,Conversation_ID,Sender_Email) VALUES  ('" + text +"',"+ Conversation_ID + ",'"+ Email_Of_Sender + "')";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    Console.WriteLine(result);
                    if (result == 1) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public Boolean InsertMatch(string Email_1, string Email_2)
        {
            if (dbCon.IsConnect())
            {
                try
                {
                    //create the match
                    string query = "INSERT INTO matches (`Match_ID`) VALUES (NULL)";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var result = cmd.ExecuteNonQuery();
                    Console.WriteLine(result);

                    //add users to the match just created
                    string query2 = "INSERT INTO match_between (Match_ID, Email) VALUES (LAST_INSERT_ID(),'" + Email_2 + "'), (LAST_INSERT_ID(),'" + Email_1 + "')";
                    Console.WriteLine(query2);
                    var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                    var result2 = cmd2.ExecuteNonQuery();
                    if (result + result2 == 3) { return true; }
                    else { return false; }
                }
                catch (Exception e)
                {
                    Console.WriteLine("shit went wrong");
                    Console.WriteLine(e);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void CloseDbCon() {
            dbCon.Close();
        }




    }
}
