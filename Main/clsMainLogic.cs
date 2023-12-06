﻿using GroupAssignmentAlonColetonWannes.Common;
using GroupAssignmentAlonColetonWannes.Items;
using GroupAssignmentAlonColetonWannes.Search;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GroupAssignmentAlonColetonWannes.Main
{
    public class clsMainLogic
    {
        private invoiceDetail activeInvoice;

        private List<string> sSQLCommands = new List<string>();




        public clsMainLogic(int invoiceNumber) {
            if(invoiceNumber == -1)
            {
                newInvoice();
                return;
            }
            int iItemCounter = 0;
            DataSet dsInvoice = clsDataAccess.ExecuteSQLStatement(clsMainSQL.getInvoice(invoiceNumber), ref iItemCounter);
         
            foreach (DataRow dataRow in dsInvoice.Tables[0].Rows)
            {
                int invoiceNum = (int)dataRow["InvoiceNum"];
                DateTime invoiceDate = (DateTime)dataRow["InvoiceDate"];
                int totalCost = (int)dataRow["TotalCost"];
                activeInvoice = new invoiceDetail(invoiceNum, invoiceDate, totalCost);
            }
            getItems();
        }

        public void newInvoice()
        {
            activeInvoice = new invoiceDetail(-1, null, 0);
            string sSQL = "SELECT MAX(InvoiceNum) FROM Invoices";
            bool res = int.TryParse(clsDataAccess.ExecuteScalarSQL(sSQL), out int newestInvoiceNumber);

            activeInvoice.InvoiceNum = newestInvoiceNumber+1;
        }
        public int getInvoiceNum()
        {
            return activeInvoice.InvoiceNum;
        }

        public DateTime? getInvoiceTime()
        {
            return activeInvoice.InvoiceDate;
        }

        public int getTotalCost()
        {
            return activeInvoice.TotalCost;
        }
     
        public ObservableCollection<itemDetail> getInvoiceItems()
        {
            return activeInvoice.InvoiceItems;
        }

        public void getItems()
        {
            string sSQL = $"SELECT LineItems.LineItemNum, LineItems.ItemCode, ItemDesc.ItemDesc, ItemDesc.Cost FROM LineItems, ItemDesc Where LineItems.ItemCode = ItemDesc.ItemCode And LineItems.InvoiceNum = {activeInvoice.InvoiceNum}";
            int iItemCounter = 0;   //Number of return values
            DataSet dsInvoiceItems = clsDataAccess.ExecuteSQLStatement(sSQL, ref iItemCounter);
            activeInvoice.InvoiceItems.Clear();
            foreach (DataRow row in dsInvoiceItems.Tables[0].Rows)
            {
                activeInvoice.InvoiceItems.Add(new itemDetail((string)row["ItemCode"], (string)row["ItemDesc"], (decimal)row["Cost"], (int)row["LineItemNum"]));
            }

        }
      

        public static BindingList<itemDetail> getItemList()
        {
            BindingList<itemDetail> items = new();

            int iItemCounter = 0;   //Number of return values
            DataSet dsItems = clsDataAccess.ExecuteSQLStatement(clsMainSQL.getItems(), ref iItemCounter);

            foreach (DataRow itemRow in dsItems.Tables[0].Rows)
            {
                string itemCode = (string)itemRow["ItemCode"];
                string itemDesc = (string)itemRow["ItemDesc"];
                decimal cost = (decimal)itemRow["Cost"];

                items.Add(new itemDetail(itemCode, itemDesc, cost));
            }

            return items;
        }


     


        public int newInvoice(DateTime? newDateTime, int newTotalCost)
        {
            string sSQL = $"INSERT INTO Invoices (InvoiceDate, TotalCost) Values (#{newDateTime}#, {newTotalCost})";
            int rowsUpdated = clsDataAccess.ExecuteNonQuery(sSQL);
            if (rowsUpdated > 0)
            {
                sSQL = "SELECT MAX(InvoiceNum) FROM Invoices";
                bool res = int.TryParse(clsDataAccess.ExecuteScalarSQL(sSQL), out int newestInvoiceNumber);

                activeInvoice.InvoiceNum = newestInvoiceNumber;
                return newestInvoiceNumber;
            }

            return -1;
        }


        public int newItem(string newItemCode)
        {
            ObservableCollection<itemDetail> x = new(activeInvoice.InvoiceItems.OrderBy(i => i.LineItemNum));
            itemDetail? y = x.LastOrDefault();
            int lineNumber = 1;
            if (y != null ) {
                lineNumber = (int)y.LineItemNum + 1;
            }

            sSQLCommands.Add(clsMainSQL.newItemInInvoice(activeInvoice.InvoiceNum, lineNumber, newItemCode));

            string sSQL = clsMainSQL.getItem(newItemCode);

            int iItem = 0;
            DataSet dsItems = clsDataAccess.ExecuteSQLStatement(sSQL, ref iItem);
            foreach (DataRow row in dsItems.Tables[0].Rows)
            {
                activeInvoice.InvoiceItems.Add(new itemDetail((string)row["ItemCode"], (string)row["ItemDesc"], (decimal)row["Cost"], lineNumber));

            }
            //string item = clsDataAccess.ExecuteScalarSQL(sSQL);

            return updateTotalCost();
        }


        public int deleteItemFromInvoice(itemDetail deletingItem, ref bool noItems)
        {
            sSQLCommands.Add(clsMainSQL.removeItem(activeInvoice.InvoiceNum, (int)deletingItem.LineItemNum));

            activeInvoice.InvoiceItems.Remove(deletingItem);
            if(activeInvoice.InvoiceItems.Count <= 0)
            {
                noItems = true;
            }
            return updateTotalCost();
        }


        public int UpdateDataBase(bool update)
        {
            if (update)
            {
                int i = 0;
                foreach (string sSQL in sSQLCommands)
                {
                    i += clsDataAccess.ExecuteNonQuery(sSQL);
                }
            }
            else
            {
                getItems();
            }
            sSQLCommands.Clear();

            int totalCost = updateTotalCost();
            clsDataAccess.ExecuteNonQuery(clsMainSQL.updateTotalCost(activeInvoice.InvoiceNum, activeInvoice.TotalCost));
            return totalCost;
        }

      


        private int updateTotalCost()
        {
            decimal totalCost = 0;
            foreach(itemDetail item in activeInvoice.InvoiceItems)
            {
                totalCost += item.Cost;
            }
            activeInvoice.TotalCost = (int)Math.Ceiling(totalCost);
            return (int)Math.Ceiling(totalCost);

        }

       
    }
}
