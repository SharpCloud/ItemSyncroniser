using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCItemSyncroniser.Models;
using Attribute = SC.API.ComInterop.Models.Attribute;
using ModelHelper = SCItemSyncroniser.Helpers.ModelHelper;
using Relationship = SC.API.ComInterop.Models.Relationship;

namespace SCItemSyncroniser.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            _masterStories = new ObservableCollection<StoryLite2>();
        }

        public string Url { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Proxy { get; set; }
        public bool ProxyAnnonymous { get; set; }
        public string ProxyUserName { get; set; }
        public string ProxyPassword { get; set; }

        public bool RememberPassword 
        { get { return _rememberPassword; }
            set
            {
                _rememberPassword = value;
                OnPropertyChanged("RememberPassword");
            } }
        private bool _rememberPassword;

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        private string _status;

        public string Logs
        {
            get { return _logs; }
            set
            {
                _logs = value;
                OnPropertyChanged("Logs");
            }
        }

        private string _logs = string.Empty;

        public void AddLog(string str)
        {
            Logs += str + "\r\n";
        }

        public StoryLite2 SelectedStory
        {
            get { return _selectedStory; }

            set
            {
                _selectedStory = value;
                OnPropertyChanged("SelectedStory");
//                OnPropertyChanged("SelectedStoryLoaded");
//                OnPropertyChanged("SelectedStoryLoadedNot");
            }
        }             
        private StoryLite2 _selectedStory;

        public bool IsLoadingSelected;
        public Story SelectedStoryFull
        {
            get { return _selectedStoryFull; }
            set
            {
                _selectedStoryFull = value;
                OnPropertyChanged("SelectedStoryFull");
            }
        }
        private Story _selectedStoryFull;


        public bool IsLoadingDonorItems;
        public ObservableCollection<Item> AllDonorItems
        {
            get { return _allDonorItems; }
            set
            {
                _allDonorItems = value;
                OnPropertyChanged("AllDonorItems");
            }
        }
        private ObservableCollection<Item> _allDonorItems;

        public ObservableCollection<Item> AllModifiedItems
        {
            get { return _allDonorItems; }
            set
            {
                _allDonorItems = value;
                OnPropertyChanged("AllModifiedItems");
            }
        }
        private ObservableCollection<Item> _allModifieItems;

        public ObservableCollection<Item> TargetItems
        {
            get { return _targetItems; }
            set
            {
                _targetItems = value;
                OnPropertyChanged("TargetItems");
            }
        }
        private ObservableCollection<Item> _targetItems;
 
        public ObservableCollection<StoryLite2> MasterStories
        {
            get { return _masterStories; }
            set
            {
                _masterStories = value;
                OnPropertyChanged("MasterStories");
            }
        }

        private ObservableCollection<StoryLite2> _masterStories;

        public bool ShowWaitForm
        {
            get { return _showWaitForm; }
            set
            {
                _showWaitForm = value;
                OnPropertyChanged("ShowWaitForm");
            }
        }
        private bool _showWaitForm;

        public async void ShowWaitFormNow(string message)
        {
            ShowWaitForm = true;
            Status = message;
            await Task.Delay(10);
        }

        public void AddMasterStory(StoryLite2 s)
        {
            if (MasterStories.FirstOrDefault(sl => sl.Id == s.Id) == null)
            {
                MasterStories.Add(s);
                OnPropertyChanged("MasterStories");
            }
        }

        public void DeleteMasterStory(StoryLite2 s)
        {
            var sf = MasterStories.FirstOrDefault(sl => sl.Id == s.Id);

            if (sf != null)
            {
                MasterStories.Remove(sf);
                OnPropertyChanged("MasterStories");
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private Dispatcher currentDispatcher;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                //check if we are on the UI thread if not switch
                if (Dispatcher.CurrentDispatcher.CheckAccess())
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                else
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action<string>(this.OnPropertyChanged), propertyName);
            }
        }

        public void SaveUserData()
        {
            ModelHelper.RegWrite("Url", Url);
            ModelHelper.RegWrite("UserName", UserName);
            ModelHelper.RegWrite("RememberPassword", RememberPassword.ToString());
            if (RememberPassword)
                ModelHelper.RegWrite("Password2", Convert.ToBase64String( Encoding.Default.GetBytes(Password)));
            ModelHelper.RegWrite("Proxy", Proxy);
            ModelHelper.RegWrite("ProxyAnnonymous", ProxyAnnonymous.ToString());
            ModelHelper.RegWrite("ProxyUserName", ProxyUserName);
            ModelHelper.RegWrite("ProxyPassword", Convert.ToBase64String(Encoding.Default.GetBytes(ProxyPassword)));
        }

        public void SaveAllData()
        {
            SaveUserData();

            string master = ModelHelper.SerializeJSON(_masterStories);
            ModelHelper.RegWrite("master", master);

            if (_selectedStory != null)
            {
                string selected = ModelHelper.SerializeJSON(_selectedStory);
                ModelHelper.RegWrite("selected", selected);
            }
        }

        public void LoadData()
        {
            ProxyPassword = Encoding.Default.GetString(Convert.FromBase64String(ModelHelper.RegRead("ProxyPassword", "")));
            ProxyUserName = ModelHelper.RegRead("ProxyUserName", "");
            ProxyAnnonymous = Boolean.Parse(ModelHelper.RegRead("ProxyAnnonymous", true.ToString()));
            Proxy = ModelHelper.RegRead("Proxy", "");
            Url = ModelHelper.RegRead("Url", "https://my.sharpcloud.com");
            UserName = ModelHelper.RegRead("UserName", "");
            RememberPassword = ModelHelper.RegRead("RememberPassword", true.ToString()) == true.ToString();

            if (RememberPassword)
            {
                Password = Encoding.Default.GetString(Convert.FromBase64String(ModelHelper.RegRead("Password2", "")));
            }
            string master = ModelHelper.RegRead("master", "[]");
            MasterStories = ModelHelper.DeserializeJSON<ObservableCollection<StoryLite2>>(master);


            SaveAllData();
        }

        public void LoadSelectedStoryFromReg()
        {
            string selected = ModelHelper.RegRead("selected", "");
            if (!string.IsNullOrEmpty(selected))
                SelectedStory = ModelHelper.DeserializeJSON<StoryLite2>(selected);
        }


        public Item SelectedModifiedItem
        {
            get { return _selectedModifiedItem; }
            set
            {
                if (value != null)
                {
                    _selectedModifiedItem = value;
                    OnPropertyChanged("SelectedModifiedItem");
                    OnPropertyChanged("ModifiedChanges");
                }
            }
        }
        private Item _selectedModifiedItem;

        public void ResetModified()
        {
            _selectedModifiedItem = null;
            OnPropertyChanged("SelectedModifiedItem");
            OnPropertyChanged("ModifiedChanges");
        }


        public ObservableCollection<string> ModifiedChanges
        {
            get
            {
                if (ItemChanges != null && SelectedModifiedItem != null)
                {
                    return new ObservableCollection<string>(ItemChanges[SelectedModifiedItem.Id]);//.ToObservableCollection();
                }

                return null; // nothing
            }
        }

        private SharpCloudApi GetApi()
        {
            return new SharpCloudApi(UserName, Password, Url, Proxy);
        }


        private Dictionary<string, List<string>> ItemChanges;

        public async void LoadDonerAndSelected()
        {
            ShowWaitForm = true;
            while (AllDonorItems == null)
            {
                LoadSourceItems();
                await Task.Delay(2000);
            }
   
            while (SelectedStoryFull == null)
            {
                LoadSelectedStory();
                await Task.Delay(1000); // we must have this loaded
            }

            ShowWaitForm = false;
        }
        
        public async void CalculateModifiedItems()
        {
            ShowWaitForm = true;
            LoadDonerAndSelected();

            while (SelectedStoryFull == null || AllDonorItems == null)
            {
                await Task.Delay(1000); // we must have this loaded
            }


            CalculateModifiedItems(AllDonorItems);
            ShowWaitForm = false;
        
        }

        private void CalculateModifiedItems(ObservableCollection<Item> sourceItems)
        {
            ItemChanges = new Dictionary<string, List<string>>();
            AllModifiedItems = new ObservableCollection<Item>();

            foreach (var item in SelectedStoryFull.Items)
            {
                Guid id;

                if (Guid.TryParse(item.ExternalId, out id))
                {
                    var sI = sourceItems.FirstOrDefault(e => e.Id == id.ToString());
                    if (sI != null)
                    {
                        if (ItemHasChanged(sI, item, ItemChanges))
                            AllModifiedItems.Add(sI);
                    }
                }
            }

        }

        private bool ItemHasChanged(Item source, Item copy, Dictionary<string, List<string>> differences )
        {
            if (source.Id == copy.ExternalId)
            {
                var list = new List<string>();
                differences.Add(source.Id, list);

                if (source.Name != copy.Name)
                    list.Add("Name has changed.");
                if (source.Description != copy.Description)
                    list.Add("Description has changed.");
                if (source.IsTransparent != copy.IsTransparent)
                    list.Add("Transparency has changed.");
                if (source.ImageId != copy.ImageId || source.ImageMode != copy.ImageMode)
                    list.Add("Image has changed.");
                if (source.StartDate != copy.StartDate)
                    list.Add("Start Date has changed.");
                if (source.DurationInDays != copy.DurationInDays)
                    list.Add("Duration has changed.");

                // tags
                foreach (var tag in source.Tags)
                {
                    if (copy.Tag_FindByName(tag.Text) == null)
                        list.Add(string.Format("Tag {0} has been added.", tag.Text));
                }

                // panels
                foreach (var panel in source.Panels)
                {
                    if (panel.IsVisible && (panel.Type == Panel.PanelType.Image || panel.Type == Panel.PanelType.RichText || panel.Type == Panel.PanelType.Video) )
                    {
                        // find matching panel
                        var pD = copy.Panel_FindByTitle(panel.Title);
                        if (pD == null)
                            list.Add(string.Format("Panel {0} has been added.",panel.Title));
                        else
                        {
                            if (pD.Data != panel.Data)
                                list.Add(string.Format("Panel {0} has been modified.", panel.Title));
                        }
                    }
                }

                // attributes
                foreach (var att in source.Story.Attributes)
                {
                    var val = source.GetAttributeValueAsText(att);
                    if (att.InUse && att.IsUserDefined && !string.IsNullOrEmpty(val) && source.GetAttributeIsAssigned(att))
                    {
                        // add a simailar attribute to the target if it is not already added
                        var newAtt = copy.Story.Attribute_FindByName(att.Name);
                        if (newAtt == null)
                        {
                            list.Add(string.Format("Attribute '{0}' does not exist in story.", att.Name));
                        }
                        else 
                        {
                            var val2 = copy.GetAttributeValueAsText(newAtt);
                            if (!copy.GetAttributeIsAssigned(newAtt))
                            {
                                list.Add(string.Format("Attribute '{0}' does not exist on the item.", att.Name));
                            }
                            else  if (val != val2)
                            {
                                if (att.Type == Attribute.AttributeType.Numeric)
                                {
                                    var vald = source.GetAttributeValueAsDouble(att);
                                    var val2d = copy.GetAttributeValueAsDouble(newAtt);

                                    if (vald != val2d)
                                        list.Add(string.Format("Attribute '{0}' has changed. {1} will replace {2}", att.Name, val, val2));

                                }
                                else
                                    list.Add(string.Format("Attribute '{0}' has changed. '{1}' will replace '{2}'", att.Name, val, val2));
                            }
                        }
                    }
                }
                // relationships
                foreach (var relationship in source.Relationships)
                {
                    var item1 = copy.Story.Item_FindByExternalId(relationship.Item1.Id);
                    var item2 = copy.Story.Item_FindByExternalId(relationship.Item2.Id);

                    if (item1 != null && item2 != null)
                    {
                        // there is a relationship in the source and both items exist in the copy

                        // does the relationship exist in the copy
                        Relationship rel;
                        if (item1.Id == copy.Id)
                        {
                            rel = copy.Relationship_FindByItem(item2);
                            if (rel == null)
                                list.Add(string.Format("Relationship missing to {0}", item2.Name));
                            else
                                CheckIfRelationshipsAreDifferent(relationship, rel, list);
                        }
                        else if (item1.Id == copy.Id)
                        {
                            rel = copy.Relationship_FindByItem(item1);
                            if (rel == null)
                                list.Add(string.Format("Relationship missing to {0}", item1.Name));
                            CheckIfRelationshipsAreDifferent(relationship, rel, list);
                        }
                    }
                }

                // resources
                foreach (var res in source.Resources)
                {
                    var resNew = copy.Resource_FindByName(res.Name);
                    if (resNew != null)
                    {
                        foreach (var tag in res.Tags)
                        {
                            if (resNew.Tag_FindByName(tag.Text) == null)
                                list.Add($"Tag '{tag.Text}' is missing on Resource '{res.Name}' on item {copy.Name}");
                        }
                    }
                    else
                    {
                        list.Add($"Resource '{res.Name}' is missing on item {copy.Name}");
                    }
                }


                for (int w = 0; w < 24; w++)
                {
                    if (source.AsElement.CanvasPoints[w].X != copy.AsElement.CanvasPoints[w].X || source.AsElement.CanvasPoints[w].Y != copy.AsElement.CanvasPoints[w].Y)
                        list.Add(string.Format("Wall position '{0}' has changed.", w+1));

                }


                return list.Any();
            }
                
            return false;
        }

        private void CheckIfRelationshipsAreDifferent(Relationship relOrig, Relationship relCopy, List<string> list)
        {

            if (relOrig.Comment != relCopy.Comment)
                list.Add($"Comment has changed on relationship between {relCopy.Item1.Name} and {relCopy.Item2.Name}");

            // check the direction
            if (relOrig.Direction != relCopy.Direction)
                list.Add($"Direction has changed on relationship between {relCopy.Item1.Name} and {relCopy.Item2.Name}");

            // check all tags
            foreach (var tag in relOrig.Tags)
            {
                if (relCopy.Tag_FindByName(tag.Text) == null)
                    list.Add($"Tag '{tag.Text}' is missing on relationship between {relCopy.Item1.Name} and {relCopy.Item2.Name}");
            }
        }


        internal void LoadSelectedStory()
        {
            if (IsLoadingSelected)
                return;

            var worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork1;
            worker.RunWorkerAsync();
        }

        internal void LoadSourceItems()
        {
            if (IsLoadingDonorItems)
                return;
            
            //open up all the stories
            var worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork2;
            worker.RunWorkerAsync();
        }

        // Worker to syncronise the stories
        void worker_DoWork1(object sender, DoWorkEventArgs e)
        {
            IsLoadingSelected = true;
            try
            {
                //Logs = "";
                var api = GetApi();

                Status = string.Format("Loading '{0}'", SelectedStory.Name);
                var sel = api.LoadStory(SelectedStory.Id);
                var perms = sel.StoryAsRoadmap.GetPermission(UserName);

                if (perms != ShareAction.owner && perms != ShareAction.admin)
                {
                    AddLog("WARNING: Requires admin permissions.");
                    Status = string.Format("Warning : Looks like you don't have admin rights.");
                    MessageBox.Show(Status);
                }
                
                SelectedStoryFull = sel;
                

                AddLog("story loaded");
                RefreshTargetItems();
                AddLog("items refreshed");

                Status = string.Format("Load completed. Story contains {0} items.", sel.Items.Count());
            }
            catch (Exception exception)
            {
                Status = exception.Message;
                AddLog(exception.Message);
            }
            IsLoadingSelected = false;
        }

        public void RefreshTargetItems()
        {
            App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
            {
                TargetItems = new ObservableCollection<Item>();

                foreach (var i in SelectedStoryFull.Items)
                {
                    if (!string.IsNullOrEmpty(i.ExternalId))
                    {
                        Guid g;
                        if (Guid.TryParse(i.ExternalId, out g)) // has a guid as an id
                            TargetItems.Add(i);
                    }
            }
            });
        }

        void worker_DoWork2(object sender, DoWorkEventArgs e)
        {
            IsLoadingDonorItems = true;
            try
            {
                //Logs = "";
                var api = GetApi();
                var master = new List<Story>();
                var masterItems = new Dictionary<string, Item>();
                var allItems = new ObservableCollection<Item>();
                Status = "started";
                foreach (var masterStory in MasterStories)
                {
                    AddLog("donor story loaded");
                    Status = string.Format("Loading '{0}'", masterStory.Name);
                    var ms = api.LoadStory(masterStory.Id);
                    master.Add(ms);
                    foreach (var i in ms.Items)
                    {
                        allItems.Add(i);
                        masterItems.Add(i.Id, i);
                    }
                }
                Status = string.Format("Load completed. {0} items loaded.", masterItems.Count);

                AddLog("ading items");
                
                
                AllDonorItems = allItems;
                AddLog("all items added");
            }
            catch (Exception exception)
            {
                Status = exception.Message;
                AddLog(exception.Message);
            }
            IsLoadingDonorItems = false;
        }
    }
}
