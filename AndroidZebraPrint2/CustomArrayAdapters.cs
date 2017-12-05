using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DakotaIntegratedSolutions
{
    class ViewHolder : Java.Lang.Object
    {
        public TextView Text { get; set; }
        public bool Selected { get; set; }
        public bool Printed { get; set; }
        public IGLNLocation Location { get; set; }
    }

    class CustomArrayAdapter : ArrayAdapter
    {
        IList objectList;
        int layoutResourceId, selectedIndex;
        Dictionary<int, bool> printedItems;
        int[] colors = new int[] { Color.LightBlue, Color.White };

        public CustomArrayAdapter(Context context, int layout, System.Collections.IList objects) : base(context, layout, objects)
        {
            objectList = objects;
            layoutResourceId = layout;
            printedItems = new Dictionary<int, bool>();
        }

        public override int Count => base.Count;

        public int GetSelectedIndex() => selectedIndex;

        public void SetSelectedIndex(int index)
        {
            selectedIndex = index;
            NotifyDataSetChanged();
        }

        public void SetPrintedIndex(int index)
        {
            try
            {
                if (printedItems.ContainsKey(index))
                {
                    printedItems.Remove(index);
                }

                printedItems.Add(index, true);
                NotifyDataSetChanged();
            }
            catch (Exception ex)
            {
                IFileUtil fileUtility = new FileUtilImplementation();
                // call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }
        }

        public override int GetItemViewType(int position) => base.GetItemViewType(position);

        public override Java.Lang.Object GetItem(int position) => base.GetItem(position);

        public override long GetItemId(int position) => base.GetItemId(position);

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder = null;
            convertView = ((LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService)).Inflate(AndroidZebraPrint2.Resource.Layout.ListRow, null);

            holder = new ViewHolder();
            var inflater = Application.Context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;

            var colorPos = position % colors.Length;
            var color = new Color(colors[colorPos]);
            convertView.SetBackgroundColor(color);

            var text = convertView.FindViewById<TextView>(AndroidZebraPrint2.Resource.Id.ListItemRowText);
            text.Text = objectList[position].ToString();
            holder.Text = text;
            holder.Selected = false;
            holder.Printed = false;
            holder.Location = (IGLNLocation)objectList[position];
            convertView.Tag = holder;

            if (selectedIndex != -1 && position == selectedIndex)
            {
                HighlightCurrentRow(holder.Text);
            }
            else
            {
                UnhighlightCurrentRow(holder.Text);
                HighlightPrintedRow(holder.Text, position);
            }

            return convertView;
        }

        void HighlightCurrentRow(View rowView)
        {
            rowView.SetBackgroundColor(Color.DarkGray);
            var textView = (TextView)rowView.FindViewById(AndroidZebraPrint2.Resource.Id.ListItemRowText);
            if (textView != null)
                textView.SetTextColor(Color.Yellow);
        }

        void UnhighlightCurrentRow(View rowView)
        {
            rowView.SetBackgroundColor(Color.Transparent);
            var textView = (TextView)rowView.FindViewById(AndroidZebraPrint2.Resource.Id.ListItemRowText);
            if (textView != null)
                textView.SetTextColor(Color.Black);
        }

        void HighlightPrintedRow(View rowView, int position)
        {
            if (printedItems.ContainsKey(position))
            {
                rowView.SetBackgroundColor(Color.ParseColor("#0A64A2"));
                var textView = (TextView)rowView.FindViewById(AndroidZebraPrint2.Resource.Id.ListItemRowText);
                if (textView != null)
                    textView.SetTextColor(Color.White);
            }
        }
    }

    class AlternateRowAdapter : ArrayAdapter
    {
        int[] colors = new int[] { Color.LightBlue, Color.White };
        IList objectList;

        public AlternateRowAdapter(Context context, int layout, System.Collections.IList objects) : base(context, layout, objects)
        {
            objectList = objects;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder = null;
            convertView = ((LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService)).Inflate(AndroidZebraPrint2.Resource.Layout.ListRow, null);
            holder = new ViewHolder();
            var inflater = Application.Context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;

            var colorPos = position % colors.Length;
            var color = new Color(colors[colorPos]);
            convertView.SetBackgroundColor(color);

            var textView = convertView.FindViewById<TextView>(AndroidZebraPrint2.Resource.Id.ListItemRowText);
            textView.Text = objectList[position].ToString();

            if (textView != null)
                textView.SetTextColor(Color.Black);

            return convertView;
        }
    }
}