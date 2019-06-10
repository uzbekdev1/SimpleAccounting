﻿using System;

namespace lg2de.SimpleAccounting
{
    public class ShellDesignViewModel : ShellViewModel
    {
        public ShellDesignViewModel()
        {
            var menuItem = new MenuViewModel(this, "c:\\Test.bxml");
            this.RecentProjects.Add(menuItem);

            var accountItem = new AccountViewModel { Identifier = 100, Name = "Kasse" };
            this.Accounts.Add(accountItem);

            var journalItem = new JournalViewModel
            {
                Identifier = 42,
                Date = DateTime.Now,
                Text = "Booking",
                Value = 123.4,
                CreditAccount = "100",
                DebitAccount = "400"
            };
            this.Journal.Add(journalItem);

            var accountJournalItem = new AccountJournalViewModel
            {
                Identifier = 42,
                Date = DateTime.Now,
                Text = "Booking",
                CreditValue = 123.4,
                RemoteAccount = "Div."
            };
            this.AccountJournal.Add(accountJournalItem);
        }
    }
}