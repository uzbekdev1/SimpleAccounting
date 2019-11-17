﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

    internal class AddBookingViewModel : Screen
    {
        private readonly ShellViewModel parent;

        private ulong creditAccount;
        private ulong debitAccount;
        private BookingTemplate selectedTemplate;

        public AddBookingViewModel(ShellViewModel parent, int bookingYear)
        {
            this.parent = parent;
            this.BookingYear = bookingYear;
        }

        public DateTime Date { get; set; } = DateTime.Today;

        public ulong BookingNumber { get; set; }

        public ObservableCollection<BookingTemplate> BindingTemplates { get; }
            = new ObservableCollection<BookingTemplate>();

        public BookingTemplate SelectedTemplate
        {
            get => this.selectedTemplate;
            set
            {
                this.selectedTemplate = value;
                if (this.selectedTemplate.Debit > 0)
                {
                    this.DebitAccount = this.selectedTemplate.Debit;
                }

                if (this.selectedTemplate.Credit > 0)
                {
                    this.CreditAccount = this.selectedTemplate.Credit;
                }

                if (this.selectedTemplate.Value > 0)
                {
                    this.BookingValue = this.selectedTemplate.Value;
                }
            }
        }

        public string BookingText { get; set; }

        public double BookingValue { get; set; }

        public List<AccountDefinition> Accounts { get; }
            = new List<AccountDefinition>();

        public ulong CreditAccount
        {
            get => this.creditAccount;
            set
            {
                if (this.creditAccount == value)
                {
                    return;
                }

                this.creditAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        public ulong DebitAccount
        {
            get => this.debitAccount;
            set
            {
                if (this.debitAccount == value)
                {
                    return;
                }

                this.debitAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        public int CreditIndex { get; set; } = -1;

        public int DebitIndex { get; set; } = -1;

        public ICommand BookCommand => new RelayCommand(_ =>
        {
            var newBooking = new AccountingDataJournalBooking
            {
                Date = this.Date.ToAccountingDate(),
                ID = this.BookingNumber
            };
            var creditValue = new BookingValue
            {
                Account = this.CreditAccount,
                Text = this.BookingText,
                Value = (int)Math.Round(this.BookingValue * 100)
            };
            var debitValue = creditValue.Clone();
            debitValue.Account = this.DebitAccount;
            newBooking.Credit = new List<BookingValue> { creditValue };
            newBooking.Debit = new List<BookingValue> { debitValue };
            this.parent.AddBooking(newBooking);

            // update for next booking
            this.BookingNumber++;
            this.NotifyOfPropertyChange(nameof(this.BookingNumber));
        },
            _ => this.Date.Year == this.BookingYear
                && this.BookingNumber > 0
                && this.BookingValue > 0
                && this.CreditIndex >= 0
                && this.DebitIndex >= 0
                && this.CreditIndex != this.DebitIndex
                && !string.IsNullOrWhiteSpace(this.BookingText));

        internal int BookingYear { get; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            this.DisplayName = "Neue Buchung erstellen";
        }
    }
}