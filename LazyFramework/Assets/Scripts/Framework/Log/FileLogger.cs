using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class FileLogger 
{
    private StreamWriter fileWriter;

    private bool roll;
    private string filename;
    int rollIndex = 1;
    private string filePath = "";

    public FileLogger(string logfile)
    {
        if (fileWriter != null)
        {
            fileWriter.Close();
            fileWriter.Dispose();
        }
        fileWriter = null;
        this.filename = logfile;

#if !SERVER
        if (Application.isMobilePlatform)
            filePath = Application.persistentDataPath + "/";
#endif

        if (!this.roll)
        {
            File.Delete(filePath + this.filename);
            fileWriter = File.AppendText(filePath + this.filename);
        }
        else
        {
            string name = filePath + this.filename.Replace(".", "_" + this.rollIndex + ".");
            File.Delete(name);
            fileWriter = File.AppendText(name);
        }

        fileWriter.AutoFlush = true;

        this.Write("******" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "******");
    }

    void Write(string content)
    {
        if (fileWriter != null && fileWriter.BaseStream != null && fileWriter.BaseStream.CanWrite)
            fileWriter.WriteLine(content);
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
            this.Write(Time + string.Format("[Exception]{0}", exception.ToString()));
    }

    public void Log(string msg)
    {
        this.Write(Time +  msg);
    }
    
    ~FileLogger()
    {
        Close();
    }

    public void Close()
    {
        if (fileWriter == null)
            return;

        fileWriter.Flush();
        fileWriter.Close();
        fileWriter = null;
    }
    
    public void Roll()
    {
        if (!this.roll)
            return;
        if (fileWriter != null)
        {
            fileWriter.Flush();
            fileWriter.Close();
            fileWriter.Dispose();
        }
        this.rollIndex++;
        fileWriter = null;
        string name = filePath + this.filename.Replace(".", "_" + this.rollIndex + ".");
        File.Delete(name);
        fileWriter = File.AppendText(name);

        fileWriter.AutoFlush = true;

        this.Write("******" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "******");
    }

    public static string Time
    {
        get
        {
            return DateTime.Now.ToString("[HH:mm:ss.ffffff zz] ");
        }
    }
}