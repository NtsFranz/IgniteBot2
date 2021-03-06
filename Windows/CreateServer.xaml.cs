﻿using Spark.Properties;
using System;
using System.Diagnostics;
using System.Windows;

namespace Spark
{
	public partial class CreateServer : Window
	{
		public CreateServer()
		{
			InitializeComponent();
		}

		private static string IndexToRegion(int index)
		{
			return index switch
			{
				0 => "uscn",
				1 => "usc",
				2 => "use",
				3 => "usw",
				4 => "euw",
				5 => "jp",
				6 => "aus",
				_ => "",
			};
		}

		private static string IndexToMap(int index)
		{
			return index switch
			{
				0 => "mpl_arena_a",
				1 => "mpl_lobby_b2",
				2 => "mpl_combat_dyson",
				3 => "mpl_combat_combustion",
				4 => "mpl_combat_fission",
				5 => "mpl_combat_gauss",
				_ => "",
			};
		}

		private void Create(object sender, RoutedEventArgs e)
		{
			// start client
			string echoPath = SparkSettings.instance.echoVRPath;
			if (!string.IsNullOrEmpty(echoPath))
			{
				// only include capturevp2 when launching in spectator
				string args = (SparkSettings.instance.chooseRegionSpectator ? (SparkSettings.instance.capturevp2 ? "-capturevp2" : "") + " -spectatorstream" : "") + 
					" -region " + IndexToRegion(SparkSettings.instance.chooseRegionIndex) +
					" -level " + IndexToMap(SparkSettings.instance.chooseMapIndex);

				try
				{
					Process.Start(echoPath, args);
				} catch (Exception ex)
				{
					Logger.LogRow(Logger.LogType.Error, $"Error opening EchoVR Process for region selection\n{ex}");
				}
			}
			else
			{
				new MessageBox(Properties.Resources.echovr_path_not_set, Properties.Resources.Error, Program.Quit).Show();
			}
			Close();
		}
	}
}
