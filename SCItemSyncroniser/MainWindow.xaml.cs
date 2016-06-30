using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using SC.API.ComInterop.Models;
using SCItemSyncroniser.Views;
using SC.API.ComInterop;
using SCItemSyncroniser.Helpers;
using SCItemSyncroniser.Models;
using SCItemSyncroniser.ViewModels;
using Attribute = SC.API.ComInterop.Models.Attribute;
using Panel = SC.API.ComInterop.Models.Panel;
using System.IO;

namespace SCItemSyncroniser
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        private CollectionViewSource sourceItems;


        public MainWindow()
        {
            InitializeComponent();

            _viewModel = DataContext as MainViewModel;
            _viewModel.LoadData();
            tbUrl.Text = _viewModel.Url;
            tbUsername.Text = _viewModel.UserName;
            tbPassword.Password = _viewModel.Password;

            sourceItems = this.FindResource("sourceItemsCvs") as CollectionViewSource;
            sourceItems.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));


            if (!ValidateCreds())
                mainTab.SelectedIndex = 0;
            else if (_viewModel.MasterStories.Count == 0) // NEED TO ADD SOME 
                mainTab.SelectedIndex = 1;
            else
                mainTab.SelectedIndex = 2;


        }

        private void SaveAndValidateCLick(object sender, RoutedEventArgs e)
        {
            if (ValidateCreds())
            {
                _viewModel.UserName = tbUsername.Text;
                _viewModel.Url = tbUrl.Text;
                _viewModel.Password = tbPassword.Password; 
                
                _viewModel.SaveAllData();
                MessageBox.Show("Well done! Your credentials have been validated.");
            }
            else
            {
                MessageBox.Show("Sorry, your credentials are not correct, please try again.");
            }
        }

        private bool ValidateCreds()
        {
            return SC.API.ComInterop.SharpCloudApi.UsernamePasswordIsValid(tbUsername.Text, tbPassword.Password,
                tbUrl.Text, _viewModel.Proxy, _viewModel.ProxyAnnonymous, _viewModel.ProxyUserName, _viewModel.ProxyPassword );
        }

        private SharpCloudApi GetApi()
        {
            _viewModel.UserName = tbUsername.Text;
            _viewModel.Url = tbUrl.Text;
            _viewModel.Password = tbPassword.Password;

            return new SharpCloudApi(_viewModel.UserName, _viewModel.Password, _viewModel.Url, _viewModel.Proxy, _viewModel.ProxyAnnonymous, _viewModel.ProxyUserName, _viewModel.ProxyPassword);
        }

        private void AddMasterStoriesClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateCreds())
            {
                MessageBox.Show("You need to validate your credentials first.");
                mainTab.SelectedIndex = 0;
                return;
            }

            var sel = new SelectStory(GetApi());

            bool? dialogResult = sel.ShowDialog();
            if (dialogResult == true)
            {
                foreach (var story in sel.SelectedStoryLites)
                {
                    _viewModel.AddMasterStory(new StoryLite2(story));
                }
                _viewModel.SaveAllData();
            }
        }

        private void RemoveMasterStory(object sender, RoutedEventArgs e)
        {
            var story = listMaster.SelectedItem as StoryLite2;
            if (story != null)
            {
                int i = listMaster.SelectedIndex;
                _viewModel.DeleteMasterStory(story);

                if (listMaster.Items.Count > 0)
                {
                    if (i == listMaster.Items.Count)
                        i--;
                    listMaster.SelectedIndex = i;
                }

                _viewModel.SaveAllData();
            }
        }

        private void SelectStoryClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateCreds())
            {
                MessageBox.Show("You need to validate your credentials first.");
                mainTab.SelectedIndex = 0;
                return;
            }

            var sel = new SelectStory(GetApi(), false, _viewModel.UserName);

            bool? dialogResult = sel.ShowDialog();
            if (dialogResult == true)
            {

                var story = sel.SelectedStoryLites.First();

                if (_viewModel.MasterStories.FirstOrDefault(m => m.Id == story.Id.ToString()) != null)
                {
                    MessageBox.Show("Selected story should not be the same as the reference stories.");
                    return;
                }

                _viewModel.SelectedStory = new StoryLite2(story);
                selectedStoryImage.Source = GetImageFromId(_viewModel.SelectedStory);

                _viewModel.SaveAllData();
            }
        }

        private void SelectLastStoryClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateCreds())
            {
                MessageBox.Show("You need to validate your credentials first.");
                mainTab.SelectedIndex = 0;
                return;
            }

            _viewModel.LoadSelectedStoryFromReg();
            selectedStoryImage.Source = GetImageFromId(_viewModel.SelectedStory);

        }

        private void Clear_OnClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedStory = null;
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            var t = sender as System.Windows.Controls.Image;
            var content = t.DataContext as StoryLite2;
            t.Source = GetImageFromId(content);
        }

        private BitmapImage GetImageFromId(StoryLite2 sl2)
        {
            if (sl2 == null) return null;
            
            String stringPath = _viewModel.Url + "/image/" + sl2.ImageId;
            Uri imageUri = new Uri(stringPath, UriKind.Absolute);
            return new BitmapImage(imageUri);
        }

        private List<Item> SelectedItems = new List<Item>();
        private List<Item> SelectedItemsSync = new List<Item>();

        private void ClickRefreshRefItems(object sender, RoutedEventArgs e)
        {
            LoadSourceItems();
        }

        private void SelectAllClicked(object sender, RoutedEventArgs e)
        {
            var ds = _viewModel.AllModifiedItems;
            _viewModel.AllModifiedItems = null;

            SelectedItemsSync.Clear();
            foreach (var item in ds)
            {
                SelectedItemsSync.Add(item);
            }

            _viewModel.AllModifiedItems = ds;
        }

        private void ClickLoadSelected(object sender, RoutedEventArgs e)
        {
            loadSelectedStory();
        }

        public async void LoadSourceItems()
        {
            lstOfAllCheckBoxs.Clear();
            _viewModel.Status = "Re-loading";
            await Task.Delay(100);

            SelectedItems.Clear();
            _viewModel.AllDonorItems = new ObservableCollection<Item>();

            _viewModel.LoadSourceItems();
            _viewModel.ShowWaitForm = true;
            await Task.Delay(100);

            while (_viewModel.AllDonorItems == null)
            {
                await Task.Delay(2000);
            }
            _viewModel.ShowWaitForm = false;
        }

        public async void loadSelectedStory()
        {
            _viewModel.Status = "Re-loading";
            await Task.Delay(100);

            _viewModel.SelectedStoryFull = null;
            _viewModel.ShowWaitForm = true;
            _viewModel.LoadSelectedStory();

            while (_viewModel.SelectedStoryFull == null)
            {
                await Task.Delay(1000);
            }
            _viewModel.ShowWaitForm = false;
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var item = cb.DataContext as Item;

            SelectedItems.Add(item);
        }

        private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var item = cb.DataContext as Item;

            SelectedItems.Remove(item);
        }

        private void ToggleButton_OnCheckedSync(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var item = cb.DataContext as Item;

            SelectedItemsSync.Add(item);
        }

        private void ToggleButton_OnUncheckedSync(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var item = cb.DataContext as Item;

            SelectedItemsSync.Remove(item);
        }

        private void CBCheck_OnLoaded(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var item = cb.DataContext as Item;

            cb.IsChecked = SelectedItemsSync.Contains(item);

        }

        private void ButtAddClick(object sender, RoutedEventArgs e)
        {
            CopySelectedItems();
        }

        private void ButtonSync(object sender, RoutedEventArgs e)
        {
            if (!SelectedItemsSync.Any())
            {
                MessageBox.Show("You have not select any items to update.");
                return;
            }
            
            UpdateSelectedItems();
        }

        public async void CopySelectedItems()
        {
            _viewModel.ShowWaitForm = true;
            _viewModel.Status = "Adding selected items";
            await Task.Delay(10);

            while (_viewModel.SelectedStoryFull == null)
            {
                _viewModel.LoadSelectedStory();
                await Task.Delay(1000);
            }

            if (_viewModel.SelectedStoryFull != null)
            {
                int count = 0;
                foreach (var selectedItem in SelectedItems)
                {
                    // make sure the item is not already added
                    if (_viewModel.SelectedStoryFull.Item_FindByExternalId(selectedItem.Id) == null)
                    {
                        count++;

                        var newItem = _viewModel.SelectedStoryFull.Item_AddNew(selectedItem.Name, false);
                        // keep a reference for future use
                        newItem.ExternalId = selectedItem.Id;

                        CopyItem(selectedItem, newItem, cbCategory.IsChecked==true, cbTags.IsChecked == true, cbPanels.IsChecked == true,
                            cbResources.IsChecked == true, cbAttributes.IsChecked == true, cbWalls.IsChecked==true);
                    }
                }
                _viewModel.Status = string.Format("{0} new items added.", count);
                // all items have been added
                // ow look at relationships

                int rels = 0;
                var dic = new Dictionary<string, Relationship>();
                if (cbRels.IsChecked == true)
                {

                    // add relationships
                    foreach (var selectedItem in SelectedItems)
                    {
                        foreach (var rel in selectedItem.Relationships)
                        {
                            if (SelectedItems.FirstOrDefault(i => (i.Id == rel.Item1.Id || i.Id == rel.Item2.Id)) !=
                                null)
                            {
                                if (AddRelationship(rel))
                                {
                                    if (!dic.ContainsKey(rel.Id))
                                    {
                                        dic.Add(rel.Id, rel);
                                        rels++;
                                    }
                                }
                            }
                        }
                    }
                }
                _viewModel.Status = string.Format("{0} new items added, {1} new relationships added", count, rels);
                _viewModel.SelectedStoryFull.Save();
                _viewModel.RefreshTargetItems();
            }
            _viewModel.ShowWaitForm = false;
        }

        private bool AddRelationship(Relationship rel)
        {
            var i1 = _viewModel.SelectedStoryFull.Item_FindByExternalId(rel.Item1.Id);
            var i2 = _viewModel.SelectedStoryFull.Item_FindByExternalId(rel.Item2.Id);

            if (i1 == null || i2==null)
                return false; // item not found
           

            var newRel = i1.Relationship_AddItem(i2, rel.Comment, rel.Direction);
            foreach (var tag in rel.Tags)
            {
                newRel.Tag_AddNew(tag.Text);
            }

            return true;
        }

        public async void UpdateSelectedItems()
        {
            _viewModel.ShowWaitForm = true;
            _viewModel.Status = "Updating selected items";
            await Task.Delay(10); 
            
            while (_viewModel.SelectedStoryFull == null)
            {
                _viewModel.LoadSelectedStory();
                await Task.Delay(1000);
            }

            int count = 0;
            if (_viewModel.SelectedStoryFull != null)
            {
                foreach (var selectedItem in SelectedItemsSync)
                {
                    var item = _viewModel.SelectedStoryFull.Item_FindByExternalId(selectedItem.Id);
                    if (item != null)
                    {
                        count++;

                        item.Name = selectedItem.Name;

                        CopyItem(selectedItem, item, false, cbSTags.IsChecked==true, cbSPanels.IsChecked==true, cbSResources.IsChecked==true, cbSAttributes.IsChecked==true, cbSWalls.IsChecked==true);
                    }
                }
                _viewModel.Status = string.Format("{0} new items updated.", count);

                int rels = 0;
                var dic = new Dictionary<string, Relationship>();
                if (cbSRelationships.IsChecked == true)
                {
                    // add relationships
                    foreach (var selectedItem in SelectedItemsSync)
                    {
                        foreach (var rel in selectedItem.Relationships)
                        {
                            if (_viewModel.SelectedStoryFull.Items.FirstOrDefault(i => (i.ExternalId == rel.Item1.Id || i.ExternalId == rel.Item2.Id)) !=
                                null)
                            {
                                if (AddRelationship(rel))
                                {
                                    if (!dic.ContainsKey(rel.Id))
                                    {
                                        dic.Add(rel.Id, rel);
                                        rels++;
                                    }
                                }
                            }
                        }
                    }
                }
                _viewModel.Status = string.Format("{0} new items added, {1} new relationships added", count, rels);
 
                
                
                _viewModel.SelectedStoryFull.Save();
                _viewModel.RefreshTargetItems();
                SelectedItemsSync.Clear();
                _viewModel.CalculateModifiedItems();
                _viewModel.ResetModified();
            }
            _viewModel.ShowWaitForm = false;
        }

        public void CopyItem(Item source, Item copy, bool bCategory, bool btags, bool bpanels, bool breses, bool battr, bool bwall)
        {
            copy.Description = source.Description;
            copy.ImageId = source.ImageId;
            copy.ImageMode = source.ImageMode;
            copy.IsTransparent = source.IsTransparent;
            copy.StartDate = source.StartDate;
            copy.DurationInDays = source.DurationInDays;

            if (bCategory)
            {
                // does the new story have the catagory?
                var cat = copy.Story.Category_FindByName(source.Category.Name);
                if (cat == null)
                {   // no, so create 
                    cat = copy.Story.Category_AddNew(source.Category.Name);
                    cat.Color = source.Category.Color;
                    cat.Description = source.Description;
                }
                // set the category to match the source (by matching on name).
                copy.Category = cat;
            }

            if (btags)
            {
                // copy tags
                foreach (var tag in source.Tags)
                {
                    copy.Tag_AddNew(tag.Text);
                }
            }

            if (breses)
            {
                // copy resources
                foreach (var resource in source.Resources)
                {
                    var newres = copy.Resource_FindByName(resource.Name);
                    if (newres == null)
                    {
                        if (resource.Type == Resource.ResourceType.File)
                        {
                            // download the file
                            var fileName = Path.GetTempFileName()+ resource.FileExtension;
                            try
                            {
                                resource.DownloadFile(fileName);
                                copy.Resource_AddFile(fileName, resource.Name);
                                File.Delete(fileName); // tidy up
                            }
                            catch (Exception e)
                            {
                                // what should we do now?
                                MessageBox.Show($"WARNING: There was an error copying the resource '{resource.Name}'.");
                            }

                        }
                        else
                            newres = copy.Resource_AddName(resource.Name, resource.Description, resource.Url.ToString());
                    }
                    else
                    {
                        newres.Description = resource.Description;
                        if (newres.Type != Resource.ResourceType.File)
                            newres.Url = resource.Url;
                    }
                    // make sure tags are allocated
                    foreach (var tag in resource.Tags)
                    {
                        newres.Tag_AddNew(tag.Text);
                    }

                }
            }

            if (battr)
            {
                // attributes
                foreach (var att in source.Story.Attributes)
                {
                    var val = source.GetAttributeValueAsText(att);
                    var vald = source.GetAttributeValueAsDouble(att);

                    if (att.InUse && att.IsUserDefined && !string.IsNullOrEmpty(val))
                    {
                        // add a simailar attribute to the target if it is not already added
                        var newAtt = copy.Story.Attribute_FindByName(att.Name);
                        if (newAtt == null)
                        {
                            // we need to  create
                            newAtt = copy.Story.Attribute_Add(att.Name, att.Type);
                            newAtt.MaxValue = att.MaxValue;
                            foreach (var attributeLabel in att.Labels)
                            {
                                newAtt.Labels_Add(attributeLabel.Text, attributeLabel.Color);
                            }
                        }
                        // TODO there is a chance at this stage that the attributes of a differnet type

                        if (source.GetAttributeIsAssigned(att))
                        {
                            if (newAtt.Type == att.Type)
                            {
                                switch (newAtt.Type)
                                {
                                    case Attribute.AttributeType.Numeric:
                                        copy.SetAttributeValue(newAtt, vald);
                                        break;
                                    case Attribute.AttributeType.List:
                                        // make sure the taget attribute has this list value
                                        var listval = newAtt.Labels_Find(val);
                                        if (listval == null)
                                            newAtt.Labels_Add(val);
                                        
                                        copy.SetAttributeValue(newAtt, val);
                                        break;
                                    case Attribute.AttributeType.Text:
                                    case Attribute.AttributeType.Date:
                                        copy.SetAttributeValue(newAtt, val);
                                        break;
                                }
                            }
                            else
                                _viewModel.AddLog(string.Format("Attribute '{0}' has a different type in each story",
                                    att.Name));

                        }
                    }
                }
            }

            if (bpanels)
            {
                // copy panels
                foreach (var panel in source.Panels)
                {
                    if (panel.IsVisible)
                    {
                        switch (panel.Type)
                        {
                            case Panel.PanelType.Image:
                            case Panel.PanelType.RichText:
                            case Panel.PanelType.Video:

                                var existingPanel = copy.Panel_FindByTitle(panel.Title);
                                if (existingPanel == null) // does not exist
                                    copy.Panel_Add(panel.Title, panel.Type, panel.Data); // create new
                                else if (existingPanel.Type == panel.Type)
                                    existingPanel.Data = panel.Data; // update 
                                break;

                            case Panel.PanelType.CustomResource:
                                var existingPanelC = copy.Panel_FindByTitle(panel.Title);
                                if (existingPanelC == null) // does not exist
                                {
                                    var newData = new ResCustomData();
                                    var data = ModelHelper.DeserializeJSON<ResCustomData>(panel.Data);
                                    newData.Text = data.Text;
                                    foreach (var guid in data.ResLinks)
                                    {
                                        var resOld = source.Resource_FindById(guid);
                                        if (resOld != null)
                                        {
                                            var resNew = copy.Resource_FindByName(resOld.Name);
                                            if (resNew != null)
                                                newData.ResLinks.Add(resNew.Id);
                                        }
                                    }

                                    copy.Panel_Add(panel.Title, panel.Type, ModelHelper.SerializeJSON(newData));
                                        // create new
                                }
                                break;

                            case Panel.PanelType.Attribute:
                                var existingPanelA = copy.Panel_FindByTitle(panel.Title);
                                if (existingPanelA == null) // does not exist
                                {
                                    var newData = new List<string>();
                                    var data = ModelHelper.DeserializeJSON<List<string>>(panel.Data);
                                    foreach (var guid in data)
                                    {
                                        var attOld = source.Story.Attribute_FindById(guid);
                                        if (attOld != null)
                                        {
                                            var attNew = copy.Story.Attribute_FindByName(attOld.Name);
                                            if (attNew != null)
                                                newData.Add(attNew.Id);
                                        }
                                    }
                                    var s = ModelHelper.SerializeJSON(newData);
                                    copy.Panel_Add(panel.Title, panel.Type, s); // create new
                                }
                                break;
                       }
                    }
                    else // hidden panel try to hide if we can
                    {
                        var existingPanel = copy.Panel_FindByTitle(panel.Title);
                        if (existingPanel != null) // does not exist
                            existingPanel.IsVisible = false;
                    }
                }
            }

            if (bwall)
            {
                // copy the wall layout
                for (int w=0; w<24; w++)
                    copy.AsElement.CanvasPoints[w] = source.AsElement.CanvasPoints[w];

            }
        
        }

        private void TabInsert_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel.SelectedStoryFull != null && _viewModel.SelectedStoryFull.Id != _viewModel.SelectedStory.Id)
                _viewModel.SelectedStoryFull = null; // reset


            switch (mainTab.SelectedIndex)
            {
                case 3:
                    _viewModel.LoadDonerAndSelected();
                    break;
                case 4:
                    _viewModel.CalculateModifiedItems();
                    break;
            }
        }

        private string _searchStr = "";
        private bool MatchOnSearch(string str)
        {
            if (!string.IsNullOrEmpty(_searchStr))
            {
                str = str.ToLower();
                return str.Contains(_searchStr);
            }
            return true;
        }
        private void DonorItemsCVSFilter(object sender, FilterEventArgs e)
        {
            var userStory = (SC.API.ComInterop.Models.Item)e.Item;
            if (MatchOnSearch(userStory.Name))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }                                                                     

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchStr = tbSearch.Text;
            sourceItems.View.Refresh();
        }


        List<CheckBox> lstOfAllCheckBoxs = new List<CheckBox>();
        private void AllItemsCheckBoxLoaded(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            var item = cb.DataContext as Item;
            cb.IsChecked = SelectedItems.Contains(item);
            lstOfAllCheckBoxs.Add(cb);
        }

        private void ClickClearPassword(object sender, RoutedEventArgs e)
        {
            tbPassword.Password = "";
            Helpers.ModelHelper.RegWrite("Password2", "");
        }

        private void ClickReloadAllStories(object sender, RoutedEventArgs e)
        {
            LoadAll();
        }

        private async void LoadAll()
        {
            _viewModel.ShowWaitForm = true;
            LoadSourceItems();
            while (_viewModel.ShowWaitForm)
                await Task.Delay(10);

            _viewModel.ShowWaitForm = true;
            loadSelectedStory();
            while (_viewModel.ShowWaitForm)
                await Task.Delay(10);
            
        }

        private void ClickSelectAll(object sender, RoutedEventArgs e)
        {
            foreach (var lstOfAllCheckBox in lstOfAllCheckBoxs)
            {
                lstOfAllCheckBox.IsChecked = true;
            }
            foreach (var item in _viewModel.AllDonorItems)
            {
                if (!SelectedItems.Contains(item))
                    SelectedItems.Add(item);
            }
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            var proxy = new ProxySettings(_viewModel);
            proxy.ShowDialog();
        }
    }
}
