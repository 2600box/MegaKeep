﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MegaKeep
{
	public partial class MegaKeep : Form
	{
		private string _local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		public MegaKeep()
		{
			InitializeComponent();
		}

		private async void btnRun_ClickAsync(object sender, EventArgs e)
		{
			txtLog.Clear();

			// first make sure megacmd is found
			if (!File.Exists(_local + "\\MEGAcmd\\mega-login.bat"))
			{
				Log("mega-login.bat was not found, please install it to the default dirctory: https://mega.nz/cmd");
				return;
			}

			// then check to make sure the file exists and it's actually a txt file
			if (!File.Exists(txtPath.Text) || txtPath.Text.Substring(txtPath.Text.LastIndexOf(".")) != ".txt")
			{
				Log("The file could not be found or is not a .txt file");
				return;
			}

			Log("Loading file...");

			// then try to read the text file's contents
			string[] lines;
			try
			{
				lines = File.ReadAllLines(txtPath.Text);
			}
			catch (Exception ex)
			{
				Log("Error: " + ex.ToString());
				return;
			}

			// run the processes in a task so it doesn't freeze the ui
			await Task.Run(() => Work(lines));
		}

		private void Work(string[] lines)
		{
			// loop through every line
			foreach (var line in lines)
			{
				var info = line.Split(':');
				var user = info[0];
				var pass = info[1];

				var restart = false;

				Log("Logging in to " + user + "...");

				Process login = new Process
				{
					StartInfo =
					{
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true,
						FileName = _local + "\\MEGAcmd\\mega-login.bat",
						Arguments = user + " \"" + pass + "\""
					}
				};

				login.Start();
				var result = login.StandardOutput.ReadToEnd();
				login.WaitForExit();

				if (login.HasExited)
				{
					if (result.Contains("Login failed"))
					{
						Log("Failed: " + result);
						continue; // just move on to the next account
					}
					else if (result.Contains("Already logged in"))
					{
						Log("Already logged in. Logging out and restarting...");
						restart = true;
					}
				}

				// wait a sec
				Thread.Sleep(1500);

				Process logout = new Process
				{
					StartInfo =
					{
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true,
						FileName = _local + "\\MEGAcmd\\mega-logout.bat"
					}
				};

				logout.Start();
				logout.WaitForExit();

				if (logout.HasExited)
					Log(logout.StandardOutput.ReadToEnd());

				if (restart)
				{
					this.Invoke((MethodInvoker) delegate
					{
						btnRun.PerformClick();
					});
					return;
				}
			}

			Log("Finished");
			File.WriteAllLines(Environment.CurrentDirectory + "\\" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-log.txt", txtLog.Lines);
		}

		private void btnLocate_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFile = new OpenFileDialog();

			openFile.Multiselect = false;
			openFile.Title = "Mega Keepalive";
			openFile.Filter = "Text Files (*.txt)|*.txt";

			if (openFile.ShowDialog() == DialogResult.OK)
			{
				txtPath.Text = openFile.FileName;
			}
		}

		private void Log(string txt)
		{
			this.Invoke((MethodInvoker) delegate
			{
				var time = "[" + DateTime.Now.ToString("hh:mm:ss tt") + "] ";

				txtLog.Text +=  time + txt + Environment.NewLine;
			});
		}

		private void MegaKeep_Load(object sender, EventArgs e)
		{
			txtPath.Text = Properties.Settings.Default.Location;
		}

		private void txtPath_TextChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.Location = txtPath.Text;
			Properties.Settings.Default.Save();
		}
	}
}
