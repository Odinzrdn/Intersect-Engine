﻿using System;
using System.Collections.Generic;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.GameObjects;

namespace Intersect.Client.UI.Game
{
    public class QuestsWindow
    {
        private Button mBackButton;
        private ScrollControl mQuestDescArea;
        private RichLabel mQuestDescLabel;
        private Label mQuestDescTemplateLabel;
        private ListBox mQuestList;

        private Label mQuestStatus;

        //Controls
        private WindowControl mQuestsWindow;

        private Label mQuestTitle;
        private Button mQuitButton;
        private QuestBase mSelectedQuest;

        //Init
        public QuestsWindow(Canvas gameCanvas)
        {
            mQuestsWindow = new WindowControl(gameCanvas, Strings.QuestLog.title, false, "QuestsWindow");
            mQuestsWindow.DisableResizing();

            mQuestList = new ListBox(mQuestsWindow, "QuestList");
            mQuestList.EnableScroll(false, true);

            mQuestTitle = new Label(mQuestsWindow, "QuestTitle");
            mQuestTitle.SetText("");

            mQuestStatus = new Label(mQuestsWindow, "QuestStatus");
            mQuestStatus.SetText("");

            mQuestDescArea = new ScrollControl(mQuestsWindow, "QuestDescription");

            mQuestDescTemplateLabel = new Label(mQuestsWindow, "QuestDescriptionTemplate");

            mQuestDescLabel = new RichLabel(mQuestDescArea);

            mBackButton = new Button(mQuestsWindow, "BackButton");
            mBackButton.Clicked += _backButton_Clicked;

            mQuitButton = new Button(mQuestsWindow, "AbandonQuestButton");
            mQuitButton.SetText(Strings.QuestLog.abandon);
            mQuitButton.Clicked += _quitButton_Clicked;

            mQuestsWindow.LoadJsonUi(GameContentManager.UI.InGame, GameGraphics.Renderer.GetResolutionString());
        }

        private void _quitButton_Clicked(Base sender, ClickedEventArgs arguments)
        {
            if (mSelectedQuest != null)
            {
                new InputBox(Strings.QuestLog.abandontitle.ToString(mSelectedQuest.Name),
                    Strings.QuestLog.abandonprompt.ToString(mSelectedQuest.Name), true, InputBox.InputType.YesNo,
                    AbandonQuest, null,
                    mSelectedQuest.Id);
            }
        }

        void AbandonQuest(object sender, EventArgs e)
        {
            PacketSender.SendCancelQuest((Guid)((InputBox) sender).UserData);
        }

        private void _backButton_Clicked(Base sender, ClickedEventArgs arguments)
        {
            mSelectedQuest = null;
            UpdateSelectedQuest();
        }

        //Methods
        public void Update(bool shouldUpdateList)
        {
            if (shouldUpdateList)
            {
                UpdateQuestList();
                UpdateSelectedQuest();
            }
            if (mQuestsWindow.IsHidden)
            {
                return;
            }

            if (mSelectedQuest != null)
            {
                if (Globals.Me.QuestProgress.ContainsKey(mSelectedQuest.Id))
                {
                    if (Globals.Me.QuestProgress[mSelectedQuest.Id].Completed && Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId == Guid.Empty)
                    {
                        //Completed
                        if (!mSelectedQuest.LogAfterComplete)
                        {
                            mSelectedQuest = null;
                            UpdateSelectedQuest();
                        }
                        return;
                    }
                    else
                    {
                        if (Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId == Guid.Empty)
                        {
                            //Not Started
                            if (!mSelectedQuest.LogBeforeOffer)
                            {
                                mSelectedQuest = null;
                                UpdateSelectedQuest();
                            }
                        }
                        return;
                    }
                }
                if (!mSelectedQuest.LogBeforeOffer)
                {
                    mSelectedQuest = null;
                    UpdateSelectedQuest();
                }
            }
        }

        private void UpdateQuestList()
        {
            mQuestList.RemoveAllRows();
            if (Globals.Me != null)
            {
                var quests = QuestBase.Lookup.Values;
                foreach (QuestBase quest in quests)
                {
                    if (quest != null)
                    {
                        if (Globals.Me.QuestProgress.ContainsKey(quest.Id))
                        {
                            if (Globals.Me.QuestProgress[quest.Id].TaskId != Guid.Empty)
                            {
                                AddQuestToList(quest.Name, Framework.GenericClasses.Color.Yellow, quest.Id);
                            }
                            else
                            {
                                if (Globals.Me.QuestProgress[quest.Id].Completed)
                                {
                                    if (quest.LogAfterComplete)
                                    {
                                        AddQuestToList(quest.Name, Framework.GenericClasses.Color.Green, quest.Id);
                                    }
                                }
                                else
                                {
                                    if (quest.LogBeforeOffer)
                                    {
                                        AddQuestToList(quest.Name, Framework.GenericClasses.Color.Red, quest.Id);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (quest.LogBeforeOffer)
                            {
                                AddQuestToList(quest.Name, Framework.GenericClasses.Color.Red, quest.Id);
                            }
                        }
                    }
                }
            }
        }

        private void AddQuestToList(string name, Framework.GenericClasses.Color clr, Guid questId)
        {
            var item = mQuestList.AddRow(name);
            item.UserData = questId;
            item.Clicked += QuestListItem_Clicked;
            item.Selected += Item_Selected;
            item.SetTextColor(clr);
        }

        private void Item_Selected(Base sender, ItemSelectedEventArgs arguments)
        {
            mQuestList.UnselectAll();
        }

        private void QuestListItem_Clicked(Base sender, ClickedEventArgs arguments)
        {
            var questNum = (Guid) ((ListBoxRow) sender).UserData;
            var quest = QuestBase.Get(questNum);
            if (quest != null)
            {
                mSelectedQuest = quest;
                UpdateSelectedQuest();
            }
            mQuestList.UnselectAll();
        }

        private void UpdateSelectedQuest()
        {
            if (mSelectedQuest == null)
            {
                mQuestList.Show();
                mQuestTitle.Hide();
                mQuestDescArea.Hide();
                mQuestStatus.Hide();
                mBackButton.Hide();
                mQuitButton.Hide();
            }
            else
            {
                mQuestDescLabel.ClearText();
                ListBoxRow rw;
                string[] myText = null;
                List<string> taskString = new List<string>();
                if (Globals.Me.QuestProgress.ContainsKey(mSelectedQuest.Id))
                {
                    if (Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId != Guid.Empty)
                    {
                        //In Progress
                        mQuestStatus.SetText(Strings.QuestLog.inprogress);
                        mQuestStatus.SetTextColor(Framework.GenericClasses.Color.Yellow, Label.ControlState.Normal);
                        if (mSelectedQuest.InProgressDescription.Length > 0)
                        {
                            mQuestDescLabel.AddText(mSelectedQuest.InProgressDescription, Framework.GenericClasses.Color.White, Alignments.Left,
                                mQuestDescTemplateLabel.Font);
                            mQuestDescLabel.AddLineBreak();
                            mQuestDescLabel.AddLineBreak();
                        }
                        mQuestDescLabel.AddText(Strings.QuestLog.currenttask, Framework.GenericClasses.Color.White, Alignments.Left,
                            mQuestDescTemplateLabel.Font);
                        mQuestDescLabel.AddLineBreak();
                        for (int i = 0; i < mSelectedQuest.Tasks.Count; i++)
                        {
                            if (mSelectedQuest.Tasks[i].Id == Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId)
                            {
                                if (mSelectedQuest.Tasks[i].Description.Length > 0)
                                {
                                    mQuestDescLabel.AddText(mSelectedQuest.Tasks[i].Description, Framework.GenericClasses.Color.White, Alignments.Left,
                                        mQuestDescTemplateLabel.Font);
                                    mQuestDescLabel.AddLineBreak();
                                    mQuestDescLabel.AddLineBreak();
                                }
                                if (mSelectedQuest.Tasks[i].Objective == QuestObjective.GatherItems) //Gather Items
                                {
                                    mQuestDescLabel.AddText(Strings.QuestLog.taskitem.ToString(
                                            Globals.Me.QuestProgress[mSelectedQuest.Id].TaskProgress,
                                            mSelectedQuest.Tasks[i].Quantity,
                                            ItemBase.GetName(mSelectedQuest.Tasks[i].TargetId)),
                                        Framework.GenericClasses.Color.White, Alignments.Left, mQuestDescTemplateLabel.Font);
                                }
                                else if (mSelectedQuest.Tasks[i].Objective == QuestObjective.KillNpcs) //Kill Npcs
                                {
                                    mQuestDescLabel.AddText(Strings.QuestLog.tasknpc.ToString(
                                            Globals.Me.QuestProgress[mSelectedQuest.Id].TaskProgress,
                                            mSelectedQuest.Tasks[i].Quantity,
                                            NpcBase.GetName(mSelectedQuest.Tasks[i].TargetId)),
                                        Framework.GenericClasses.Color.White, Alignments.Left, mQuestDescTemplateLabel.Font);
                                }
                            }
                        }
                        if (mSelectedQuest.Quitable)
                        {
                            mQuitButton.Show();
                        }
                    }
                    else
                    {
                        if (Globals.Me.QuestProgress[mSelectedQuest.Id].Completed)
                        {
                            //Completed
                            if (mSelectedQuest.LogAfterComplete)
                            {
                                mQuestStatus.SetText(Strings.QuestLog.completed);
                                mQuestStatus.SetTextColor(Framework.GenericClasses.Color.Green, Label.ControlState.Normal);
                                mQuestDescLabel.AddText(mSelectedQuest.EndDescription, Framework.GenericClasses.Color.White, Alignments.Left,
                                    mQuestDescTemplateLabel.Font);
                            }
                        }
                        else
                        {
                            //Not Started
                            if (mSelectedQuest.LogBeforeOffer)
                            {
                                mQuestStatus.SetText(Strings.QuestLog.notstarted);
                                mQuestStatus.SetTextColor(Framework.GenericClasses.Color.Red, Label.ControlState.Normal);
                                mQuestDescLabel.AddText(mSelectedQuest.BeforeDescription, Framework.GenericClasses.Color.White, Alignments.Left,
                                    mQuestDescTemplateLabel.Font);
                                mQuitButton?.Hide();
                            }
                        }
                    }
                }
                else
                {
                    //Not Started
                    if (mSelectedQuest.LogBeforeOffer)
                    {
                        mQuestStatus.SetText(Strings.QuestLog.notstarted);
                        mQuestStatus.SetTextColor(Framework.GenericClasses.Color.Red, Label.ControlState.Normal);
                        mQuestDescLabel.AddText(mSelectedQuest.BeforeDescription, Framework.GenericClasses.Color.White, Alignments.Left,
                            mQuestDescTemplateLabel.Font);
                    }
                }
                mQuestList.Hide();
                mQuestTitle.IsHidden = false;
                mQuestTitle.Text = mSelectedQuest.Name;
                mQuestDescArea.IsHidden = false;
                mQuestDescLabel.Width = mQuestDescArea.Width - mQuestDescArea.GetVerticalScrollBar().Width;
                mQuestDescLabel.SizeToChildren(false, true);
                mQuestStatus.Show();
                mBackButton.Show();
            }
        }

        public void Show()
        {
            mQuestsWindow.IsHidden = false;
        }

        public bool IsVisible()
        {
            return !mQuestsWindow.IsHidden;
        }

        public void Hide()
        {
            mQuestsWindow.IsHidden = true;
            mSelectedQuest = null;
        }
    }
}