using Dotmim.Sync;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.Sqlite;
using Dotmim.Sync.Web.Client;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace OfflineSyncMobileApp
{

    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {

        public string ClientDBconnectionString = FileSystem.AppDataDirectory + "advworks.db";
        public string ServerDBconnectionString = "http://stechbuzz.somee.com/dotmain/api/sync";

        SqliteSyncProvider clientProvider;
        WebClientOrchestrator serverOrchestrator;

        public MainPage()
        {
            InitializeComponent();

            clientProvider = new SqliteSyncProvider(ClientDBconnectionString);
            serverOrchestrator = new WebClientOrchestrator(ServerDBconnectionString);
        }

        private async void SynchronizeAsync()
        {
            try
            {

                

                var syncTables = new string[] { "ItemMaster"};

                SyncSetup syncSetup = new SyncSetup(syncTables);
                syncSetup.Tables["ItemMaster"].SyncDirection = SyncDirection.Bidirectional;
                // Filter columns
                syncSetup.Tables["ItemMaster"].Columns.AddRange(new string[] { "ItemId", "ItemName" });

                // Create a filter on table
                var ItemMasterTableFilter = new SetupFilter("ItemMaster");

                // Conflict resolution MUST BE set to ServerWins
                //SyncOptions syncOptions = null;
                //syncOptions.ConflictResolutionPolicy = ConflictResolutionPolicy.ServerWins;

                // Creating an agent that will handle all the process
                var agent = new SyncAgent(clientProvider, serverOrchestrator, syncTables);

                // Using the IProgress<T> pattern to handle progession dring the synchronization
                var progress = new SynchronousProgress<ProgressArgs>(args => txtOut.Text = txtOut.Text + "\n\n" + (args.Context.SyncStage+ ":" + args.Message));

                //define sync type
                SyncType type = SyncType.Normal;
                if (txtOut.Text != "")
                {
                    type = SyncType.ReinitializeWithUpload; 
                }

                //sync
                var s1 = await agent.SynchronizeAsync(type, progress);

                txtOut.Text = txtOut.Text + "\n\n" + s1.ToString();

                
            }
            catch (Exception ex)
            {
                txtOut.Text = ex.ToString();
            }
            finally
            {
                btnSync.IsEnabled = true;

                busyIndi.IsVisible = false;
            }


        }


        private void btnSync_Clicked(object sender, EventArgs e)
        {
            btnSync.IsEnabled = false;
            busyIndi.IsVisible = true;
            txtOut.Text = "wait..";
            SynchronizeAsync();
        }

        private void btnSave_Clicked(object sender, EventArgs e)
        {
            try
            {
                btnSave.IsEnabled = false;
                busyIndi.IsVisible = true;

                string statement = "Insert Into ItemMaster(ItemName) VALUES(@ItemName)";
                bool res = SQLliteInsert(statement);

                if (res)
                {
                    txtOut.Text = "Row inserted";
                }
                else
                {
                    txtOut.Text = "Row insert failed";
                }

            }
            catch (Exception ex)
            {
                txtOut.Text = ex.ToString();
            }
            finally
            {
                btnSync.IsEnabled = true;
                btnSave.IsEnabled = true;
                busyIndi.IsVisible = false;
                txtvalue.Text = "";
            }
        }

        private void btnReadDB_Clicked(object sender, EventArgs e)
        {
            try
            {
                var conn = clientProvider.CreateConnection();
                string statement = "SELECT ItemId, ItemName FROM ItemMaster";
                SqliteConnection con = new SqliteConnection(conn.ConnectionString);
                con.Open();

                SqliteCommand cmd = new SqliteCommand(statement, con);

                SqliteDataReader rdr = cmd.ExecuteReader();
                txtOut.Text = "";
                while (rdr.Read())
                {
                    
                    txtOut.Text = txtOut.Text + "\n\n" + rdr.GetString(0) +":" + rdr.GetString(1);
                    //Console.WriteLine($"{rdr.GetInt32(0)} {rdr.GetString(1)} {rdr.GetInt32(2)}");
                }


                con.Close();

            }
            catch (Exception ex)
            {
                txtOut.Text = ex.ToString();
            }
        }

        private bool SQLliteInsert(string statement)
        {
            try
            {
                var conn = clientProvider.CreateConnection();

                

                SqliteConnection con = new SqliteConnection(conn.ConnectionString);
                con.Open();

                SqliteCommand cmd = new SqliteCommand(statement, con);

                cmd.Parameters.AddWithValue("@ItemName", txtvalue.Text.ToString());
                cmd.Prepare();

                cmd.ExecuteNonQuery();

                con.Close();

                return true;
            }
            catch (Exception ex)
            {
                txtOut.Text = ex.ToString();
                return false;
            }
        }
    }
}
