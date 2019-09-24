﻿// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Input;
    using Caliburn.Micro;
    using CsvHelper;
    using lg2de.SimpleAccounting.Extensions;
    using lg2de.SimpleAccounting.Model;

    internal class ImportBookingsViewModel : Screen
    {
        private readonly ShellViewModel parent;
        private readonly List<AccountingDataMapping> importMappings;
        private ulong importAccount;

        public ImportBookingsViewModel(ShellViewModel parent, List<AccountingDataMapping> importMappings)
        {
            this.parent = parent;
            this.importMappings = importMappings ?? new List<AccountingDataMapping>();

            this.DisplayName = "Import von Kontodaten";
        }

        public List<AccountDefinition> Accounts { get; }
            = new List<AccountDefinition>();

        public DateTime RangeMin { get; internal set; }

        public DateTime RangMax { get; internal set; }

        public AccountingDataJournal Journal { get; internal set; }

        public ulong BookingNumber { get; internal set; }

        public ulong ImportAccount
        {
            get => this.importAccount;
            set
            {
                if (this.importAccount == value)
                {
                    return;
                }

                this.importAccount = value;
                this.NotifyOfPropertyChange();
            }
        }

        public AccountDefinition SelectedAccount { get; set; }

        public ObservableCollection<ImportEntryViewModel> ImportData { get; }
            = new ObservableCollection<ImportEntryViewModel>();

        public ICommand LoadDataCommand => new RelayCommand(_ =>
        {
            using (var openFileDialog = new System.Windows.Forms.OpenFileDialog())
            {
                openFileDialog.Filter = "Booking data files (*.csv)|*.csv";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                using (var reader = new StreamReader(openFileDialog.FileName, Encoding.GetEncoding(1252)))
                {
                    this.ImportBookings(reader);
                }
            }

        }, _ => this.SelectedAccount != null);

        public ICommand BookAllCommand => new RelayCommand(
            _ => this.ProcessData(),
            _ => this.ImportData.All(x => x.RemoteAccount != null));

        public ICommand BookMappedCommand => new RelayCommand(
            _ => this.ProcessData(),
            _ => this.ImportData.Any(x => x.RemoteAccount != null));

        internal void ImportBookings(TextReader reader)
        {
            this.ImportData.Clear();

            var lastEntry = this.Journal.Booking
                .Where(x => x.Credit.Any(c => c.Account == this.ImportAccount) || x.Debit.Any(c => c.Account == this.ImportAccount))
                .OrderBy(x => x.Date)
                .LastOrDefault();
            if (lastEntry != null)
            {
                this.RangeMin = lastEntry.Date.ToDateTime() + TimeSpan.FromDays(1);
            }

            using (var csv = new CsvReader(reader))
            {
                csv.Read();
                var header = csv.ReadHeader();
                while (csv.Read())
                {
                    var dateField = this.SelectedAccount.ImportMapping
                        .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingTarget.Date)?.Source;
                    csv.TryGetField(dateField, out DateTime date);
                    if (date < this.RangeMin || date > this.RangMax)
                    {
                        continue;
                    }

                    var nameField = this.SelectedAccount.ImportMapping
                        .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingTarget.Name);
                    var textField = this.SelectedAccount.ImportMapping
                        .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingTarget.Text);
                    var valueField = this.SelectedAccount.ImportMapping
                        .FirstOrDefault(x => x.Target == AccountDefinitionImportMappingTarget.Value)?.Source;

                    csv.TryGetField<string>(nameField?.Source, out var name);
                    csv.TryGetField<string>(textField?.Source, out var text);
                    csv.TryGetField<double>(valueField, out var value);

                    if (!string.IsNullOrEmpty(textField?.IgnorePattern))
                    {
                        text = Regex.Replace(text, textField?.IgnorePattern, string.Empty);
                    }

                    var item = new ImportEntryViewModel
                    {
                        Date = date,
                        Accounts = this.Accounts,
                        Identifier = this.BookingNumber++,
                        Name = name,
                        Text = text,
                        Value = value
                    };

                    var longValue = (long)(value * 100);
                    foreach (var importMapping in this.importMappings)
                    {
                        if (!Regex.IsMatch(text, importMapping.TextPattern))
                        {
                            // mapping does not match
                            continue;
                        }

                        if (importMapping.ValueSpecified && longValue != importMapping.Value)
                        {
                            // mapping does not match
                            continue;
                        }

                        // use first match
                        item.RemoteAccount = this.Accounts.SingleOrDefault(a => a.ID == importMapping.AccountID);
                        break;
                    }

                    this.ImportData.Add(item);
                }
            }
        }

        internal void ProcessData()
        {
            foreach (var item in this.ImportData)
            {
                if (item.RemoteAccount == null)
                {
                    // mapping missing - abort
                    break;
                }

                var newBooking = new AccountingDataJournalBooking
                {
                    Date = item.Date.ToAccountingDate(),
                    ID = item.Identifier
                };
                var creditValue = new BookingValue
                {
                    Text = $"{item.Name} - {item.Text}",
                    Value = (int)Math.Abs(Math.Round(item.Value * 100))
                };
                var debitValue = creditValue.Clone();
                if (item.Value > 0)
                {
                    creditValue.Account = item.RemoteAccount.ID;
                    debitValue.Account = this.ImportAccount;
                }
                else
                {
                    creditValue.Account = this.ImportAccount;
                    debitValue.Account = item.RemoteAccount.ID;
                }

                newBooking.Credit = new List<BookingValue> { creditValue };
                newBooking.Debit = new List<BookingValue> { debitValue };
                this.parent.AddBooking(newBooking);
            }

            this.TryClose(null);
        }
    }
}