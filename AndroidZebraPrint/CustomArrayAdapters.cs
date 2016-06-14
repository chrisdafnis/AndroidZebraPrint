
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AndroidZebraPrint
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
        private IList objectList;
        private int layoutResourceId;
        private int selectedIndex;
        private Dictionary<int, bool> printedItems;
        private int[] colors = new int[] { Color.LightBlue, Color.White };

        public CustomArrayAdapter(Context context, int layout, System.Collections.IList objects) : base(context, layout, objects)
        {
            objectList = objects;
            layoutResourceId = layout;
            printedItems = new Dictionary<int, bool>();
        }

        public override int Count
        {
            get
            {
                return base.Count;
            }
        }

        public int GetSelectedIndex()
        {
            return selectedIndex;
        }

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
                //call LogFile method and pass argument as Exception message, event name, control name, error line number, current form name
                fileUtility.LogFile(ex.Message, ex.ToString(), MethodBase.GetCurrentMethod().Name, ExceptionHelper.LineNumber(ex), Class.SimpleName);
            }
        }

        public override int GetItemViewType(int position)
        {
            return base.GetItemViewType(position);
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return base.GetItem(position);
        }

        public override long GetItemId(int position)
        {
            return base.GetItemId(position);
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder = null;
            convertView = ((LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.ListRow, null);

            holder = new ViewHolder();
            var inflater = Application.Context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;
            
            int colorPos = position % colors.Length;
            Color color = new Color(colors[colorPos]);
            convertView.SetBackgroundColor(color);

            TextView text = convertView.FindViewById<TextView>(Resource.Id.ListItemRowText);
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

        private void HighlightCurrentRow(View rowView)
        {
            rowView.SetBackgroundColor(Color.DarkGray);
            TextView textView = (TextView)rowView.FindViewById(Resource.Id.ListItemRowText);
            if (textView != null)
                textView.SetTextColor(Color.Yellow);
        }

        private void UnhighlightCurrentRow(View rowView)
        {
            rowView.SetBackgroundColor(Color.Transparent);
            TextView textView = (TextView)rowView.FindViewById(Resource.Id.ListItemRowText);
            if (textView != null)
                textView.SetTextColor(Color.Black);
        }

        private void HighlightPrintedRow(View rowView, int position)
        {
            if (printedItems.ContainsKey(position))
            {
                rowView.SetBackgroundColor(Color.ParseColor("#0A64A2"));
                TextView textView = (TextView)rowView.FindViewById(Resource.Id.ListItemRowText);
                if (textView != null)
                    textView.SetTextColor(Color.White);
            }
        }
    }

    class AlternateRowAdapter : ArrayAdapter
    {
        private int[] colors = new int[] { Color.LightBlue, Color.White};
        private IList objectList;

        public AlternateRowAdapter(Context context, int layout, System.Collections.IList objects) : base(context, layout, objects)
        {
            objectList = objects;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder = null;
            convertView = ((LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.ListRow, null);
            holder = new ViewHolder();
            var inflater = Application.Context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;

            int colorPos = position % colors.Length;
            Color color = new Color(colors[colorPos]);
            convertView.SetBackgroundColor(color);

            TextView textView = convertView.FindViewById<TextView>(Resource.Id.ListItemRowText);
            textView.Text = objectList[position].ToString();

            if (textView != null)
                textView.SetTextColor(Color.Black);

            return convertView;
        }
    }
}