﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;

namespace ExtLibrary
{
    /// <summary>
    /// 带下拉列表的自定义搜索文本框
    /// </summary>
	[ToolboxItem( true )]
	public partial class CustomCompleteTextBox : TextBox
    {
        /// <summary>
        /// 监视鼠标滚轮事件
        /// </summary>
        private MouseWheelFilter mouseWheel;

        /// <summary>
        /// 监视鼠标左,中,右键点击事件
        /// </summary>
        private AppClickFilter appClick;

        /// <summary>
        /// 内部使用,用于存储listBox数据
        /// </summary>
        private ListBox innerListBox;
        /// <summary>
        /// 显示候选列表
        /// </summary>
        private ListBox box;
		private ToolStripControlHost host;

        /// <summary>
        /// 下拉控件
        /// </summary>
		private ToolStripDropDownExt drop;

        //--------------------------------------------------------------------------------

		/// <summary>
		/// 获取或设置数据集合
		/// </summary>
		public ListBox.ObjectCollection Items
		{
			get
            {
                return this.innerListBox.Items;
            }
		}

        /// <summary>
        /// 获取或设置选择的项目
        /// </summary>
        public object SelectedItem
        {
            get
            {
                return this.innerListBox.SelectedItem;
            }
            set
            {
                this.innerListBox.SelectedItem = value;
                this.SetText();
            }
        }

        /// <summary>
        /// 获取或设置显示的属性
        /// </summary>
        public string DisplayMember
        {
            get
            {
                return this.innerListBox.DisplayMember;
            }
            set
            {
                this.innerListBox.DisplayMember = value;
            }
        }

        /// <summary>
        /// 获取或设置值的属性
        /// </summary>
        public string ValueMember
        {
            get
            {
                return this.innerListBox.ValueMember;
            }
            set
            {
                this.innerListBox.ValueMember = value;
            }
        }

        /// <summary>
        /// 获取或设置是否自动显示下拉列表
        /// </summary>
        public bool AutoDrop
        {
            get;
            set;
        }

        //--------------------------------------------------------------------------------

		/// <summary>
		/// 构造函数
		/// </summary>
		public CustomCompleteTextBox()
			: base()
		{
			this.InitControl();
		}

        //--------------------------------------------------------------------------------

        /// <summary>
        /// 初始化布局
        /// </summary>
		protected override void InitLayout()
		{
			base.InitLayout();

			this.box.Width = this.Width - 2;
		}

		protected override void OnClick( EventArgs e )
        {
            base.OnClick( e );
            this.SelectAll();
            this.Focus();

            if ( this.AutoDrop )
            {
                this.DropList();
            }
        }

		protected override void OnEnter( EventArgs e )
		{
			base.OnEnter( e );
            this.SelectAll();
            this.mouseWheel.Enable = true;

            if ( this.AutoDrop )
            {
                this.DropList();
            }
        }

		protected override void OnLeave( EventArgs e )
		{
			base.OnLeave( e );
            this.mouseWheel.Enable = false;
            this.innerListBox.SelectedItem = OnlyOneMatch( this.Text );

            if ( this.AutoDrop )
            {
                this.CloseList();
            }
		}

        protected override void OnTextChanged( EventArgs e )
        {
            base.OnTextChanged( e );
            
            this.innerListBox.SelectedItem = OnlyOneMatch( this.Text );

            if ( this.AutoDrop )
            {
                this.DropList();
            }
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            //去掉系统提示音
            if ( e.KeyChar == 13 )
            {
                e.Handled = true;
            }

            base.OnKeyPress( e );
        }

        protected override void OnKeyDown( KeyEventArgs e )
        {
            //按上下键时不改变文本框内的光标位置
            switch ( e.KeyCode )
            {
                case Keys.Up:
                case Keys.Down:
                    e.Handled = true;
                    break;
            }

            base.OnKeyDown( e );
        }

        protected override void WndProc( ref Message m )
        {
            //上下键,回车键,将消息转发到下拉框
            if ( m.Msg == 0x100 )
            {
                switch ( m.WParam.ToInt32() )
                {
                    case 13:
                    case 38:
                    case 40:
                        if ( this.box.Visible )
                        {
                            WindowsAPI.SendMessage( this.box.Handle, m.Msg, m.WParam, m.LParam );
                        }
                        break;
                }
            }

            base.WndProc( ref m );
        }

        //--------------------------------------------------------------------------------

        /// <summary>
        /// 显示下拉列表
        /// </summary>
        public void DropList()
        {
            this.box.Items.Clear();
            this.box.DisplayMember = this.DisplayMember;
            this.box.ValueMember = this.ValueMember;

            if ( this.Items != null )
            {
                if ( string.IsNullOrEmpty( this.Text ) )
                {
                    this.box.Items.AddRange( this.Items );
                }
                else
                {
                    for ( int i = 0; i < this.Items.Count; i++ )
                    {
                        object obj = this.Items[i];

                        if ( obj != null )
                        {
                            if ( this.innerListBox.GetItemText( obj ).Contains( this.Text ) )
                            {
                                this.box.Items.Add( obj );
                            }
                        }
                    }
                }

                this.box.SelectedItem = this.SelectedItem;
            }

            if ( !this.drop.Visible )
            {
                Screen screent = Screen.FromControl( this );
                Point showPoint = new Point( 0 - (this.Size.Width - this.ClientSize.Width) / 2, this.Height - (this.Size.Height - this.ClientSize.Height) / 2 );
                ToolStripDropDownDirection direction = this.drop.Height > screent.WorkingArea.Height - this.PointToScreen( showPoint ).Y ? ToolStripDropDownDirection.AboveRight : ToolStripDropDownDirection.BelowRight;
                showPoint = direction == ToolStripDropDownDirection.BelowRight ? showPoint : new Point( 0 - (this.Size.Width - this.ClientSize.Width) / 2, 0 - (this.Size.Height - this.ClientSize.Height) / 2 );
                this.drop.Show( this, showPoint, direction );
            }
        }

        /// <summary>
        /// 关闭下拉列表
        /// </summary>
        public void CloseList()
        {
            this.drop.Close();
        }
        

        private void SetText()
        {
            this.Text = this.box.GetItemText( this.SelectedItem );
            this.SelectionStart = this.Text.Length;
        }

        private object OnlyOneMatch( string text )
        {
            object result = null;
            int count = 0;

            if ( this.Items != null )
            {
                for ( int i = 0; i < this.Items.Count; i++ )
                {
                    object obj = this.Items[i];

                    if ( obj != null )
                    {
                        if ( this.innerListBox.GetItemText( obj ) == this.Text )
                        {
                            result = obj;
                            count++;
                        }
                    }
                }
            }

            return count == 1 ? result : null;
        }

        //--------------------------------------------------------------------------------

        /// <summary>
        /// 初始化各参数
        /// </summary>
        private void InitControl()
		{
            this.AutoDrop = true;
            this.innerListBox = new ListBox();
            this.innerListBox.SelectionMode = SelectionMode.One;

            this.box = new ListBox();
			this.box.Margin = Padding.Empty;
			this.box.BorderStyle = BorderStyle.None;
			this.box.TabStop = false;
			this.box.SelectionMode = SelectionMode.One;
			this.box.IntegralHeight = false;
			this.box.MouseMove += Box_MouseMove;
			this.box.Click += Box_Click;
            this.box.KeyDown += Box_KeyDown;

			this.host = new ToolStripControlHost( box );
			this.host.Margin = Padding.Empty;
			this.host.Padding = Padding.Empty;
			this.host.AutoSize = false;
            this.host.AutoToolTip = false;

			this.drop = new ToolStripDropDownExt();
			this.drop.AutoClose = false;
			this.drop.Items.Add( host );
			this.drop.Margin = Padding.Empty;
			this.drop.Padding = new Padding( 1 );
			this.drop.ShowItemToolTips = false;
			this.drop.TabStop = false;
			this.drop.Closed += Drop_Closed;
			this.drop.ActiveChange += Drop_ActiveChange;

            this.mouseWheel = new MouseWheelFilter( this.box );
            this.mouseWheel.Enable = false;
            this.appClick = new AppClickFilter( () =>
            {
                if ( this.AutoDrop )
                {
                    this.CloseList();
                }
            }, this, this.drop, this.box );
            Application.AddMessageFilter( this.mouseWheel );
            Application.AddMessageFilter( this.appClick );
        }
        
        
        /// <summary>
        /// 关闭下拉列表时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Drop_Closed( object sender, ToolStripDropDownClosedEventArgs e )
        {
            this.box.SelectedIndex = -1;
        }

        /// <summary>
        /// 下拉列表激活或失去激活状态时引发的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Drop_ActiveChange( object sender, ActiveChangeEventArgs e )
        {
            if ( this.AutoDrop && !e.Active )
            {
                this.CloseList();
            }
        }

        /// <summary>
        /// 在 listbox 上按下按键时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Box_KeyDown( object sender, KeyEventArgs e )
        {
            switch ( e.KeyCode )
            {
                case Keys.Enter:
                    this.Box_Click( this.box, EventArgs.Empty );
                    break;
            }
        }

        /// <summary>
        /// 单击下拉选项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void Box_Click( object sender, EventArgs e )
		{
            if ( this.box.SelectedItem != null )
            {
                this.SelectedItem = this.box.SelectedItem;
            }

            this.CloseList();
        }

        /// <summary>
        /// 鼠标在选项间移动时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void Box_MouseMove( object sender, MouseEventArgs e )
		{
            int index = this.box.IndexFromPoint( e.Location );

            if ( index > -1 )
            {
                this.box.SelectedIndex = index;
            }
		}
	}
}
