﻿using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Spark
{
	public class ReplayFile
	{
		public int nframes;
		public string filename;
		public List<string> rawFrames;
		public List<g_Instance> frames { private get; set; }

		/// <summary>
		/// Gets or converts the requested frame.
		/// May return null if the frame can't be converted.
		/// </summary>
		public g_Instance GetFrame(int index)
		{
			if (frames[index] != null) return frames[index];

			// repeat because maybe the requested frame needs to be discarded.
			while (rawFrames.Count > 0)
			{
				g_Instance newFrame = ReplayFileReader.FromEchoReplayString(rawFrames[index]);
				if (newFrame != null)
				{
					frames[index] = newFrame;
					return frames[index];
				}

				Logger.LogRow(Logger.LogType.Error, $"Discarded frame {index}");
				frames.RemoveAt(index);
				rawFrames.RemoveAt(index);
				nframes--;
			}
			Logger.LogRow(Logger.LogType.Error, "File contains no valid arena frames.");
			return null;
		}
	}


	internal class ReplayFileReader
	{
		public float fileReadProgress = 0;
		public ReplayFile replayFile;
		public object replayFileLock = new object();


		/// <summary>
		/// Part of the process for reading the file
		/// </summary>
		/// <param name="replayFilePath">The full filepath of the replay file</param>
		/// <param name="processFrames"></param>
		public async Task<ReplayFile> LoadFileAsync(string replayFilePath = "", bool processFrames = false)
		{
			if (string.IsNullOrEmpty(replayFilePath)) return null;

			Logger.LogRow(Logger.LogType.Info, "Reading file: " + replayFilePath);
			StreamReader reader = new StreamReader(replayFilePath);

			Thread loadThread = new Thread(() => ReadReplayFile(reader, replayFilePath));
			loadThread.Start();
			while (loadThread.IsAlive)
			{
				// maybe put a progress bar here
				await Task.Delay(10);
			}

			//if (processFrames)
			//{
			//	Thread processTemporalDataThread = new Thread(() => ProcessAllTemporalData(loadedDemo));
			//	processTemporalDataThread.Start();
			//}

			return replayFile;
		}


		/// <summary>
		/// Actually reads the replay file into memory
		/// </summary>
		private void ReadReplayFile(StreamReader fileReader, string filename)
		{
			using (fileReader = OpenOrExtract(fileReader))
			{
				fileReadProgress = 0;
				List<string> allLines = new List<string>();
				do
				{
					allLines.Add(fileReader.ReadLine());
					fileReadProgress += .0001f;
					fileReadProgress %= 1;
				} while (!fileReader.EndOfStream);

				//string fileData = fileReader.ReadToEnd();
				//List<string> allLines = fileData.LowMemSplit("\n");

				ReplayFile game = new ReplayFile
				{
					rawFrames = allLines,
					nframes = allLines.Count,
					filename = filename,
					frames = new List<g_Instance>(new g_Instance[allLines.Count])
				};

				lock (replayFileLock)
				{
					replayFile = game;
				}
			}
		}


		private static StreamReader OpenOrExtract(StreamReader reader)
		{
			char[] buffer = new char[2];
			reader.Read(buffer, 0, buffer.Length);
			reader.DiscardBufferedData();
			reader.BaseStream.Seek(0, SeekOrigin.Begin);
			if (buffer[0] != 'P' || buffer[1] != 'K') return reader;
			
			ZipArchive archive = new ZipArchive(reader.BaseStream);
			StreamReader ret = new StreamReader(archive.Entries[0].Open());
			//reader.Close();
			return ret;
		}
		
		public static g_Instance FromEchoReplayString(string line)
		{
			if (!string.IsNullOrEmpty(line))
			{
				string[] splitJSON = line.Split('\t');
				string onlyJSON, onlyTime;
				if (splitJSON.Length == 2)
				{
					onlyJSON = splitJSON[1];
					onlyTime = splitJSON[0];
				}
				else
				{
					Logger.LogRow(Logger.LogType.Error, "Row doesn't include both a time and API JSON");
					return null;
				}
				DateTime frameTime = DateTime.Parse(onlyTime);

				// if this is actually valid arena data
				if (onlyJSON.Length > 800)
				{
					return FromJSON(frameTime, onlyJSON);
				}

				Logger.LogRow(Logger.LogType.Error, "Row is not arena data.");
				return null;
			}

			Logger.LogRow(Logger.LogType.Error, "String is empty");
			return null;
		}
		
		/// <summary>
		/// Creates a frame from json and a timestamp
		/// </summary>
		/// <param name="time">The time the frame was recorded</param>
		/// <param name="json">The json for the frame</param>
		/// <returns>A Frame object</returns>
		private static g_Instance FromJSON(DateTime time, string json)
		{
			g_Instance frame = JsonConvert.DeserializeObject<g_Instance>(json);
			if (frame == null) return null;
			
			frame.recorded_time = time;
			return frame;
		}
	}
}
