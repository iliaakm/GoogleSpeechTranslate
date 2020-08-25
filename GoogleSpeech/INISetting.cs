using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class INISetting
{
  private static Dictionary<string, INISetting.INI_Values> groups = new Dictionary<string, INISetting.INI_Values>();
  private static string defaultGroup = "Default";

  static INISetting()
  {
    if (!File.Exists("settings.ini"))
      return;
    string[] strArray1 = File.ReadAllLines("settings.ini");
    string key1 = INISetting.defaultGroup;
    for (int index = 0; index < strArray1.Length; ++index)
    {
      if (!string.IsNullOrEmpty(strArray1[index]) && !strArray1[index].TrimStart().StartsWith("#"))
      {
        if (strArray1[index].TrimStart().StartsWith("[") && strArray1[index].Contains("]"))
        {
          int startIndex = strArray1[index].IndexOf("[") + 1;
          int num = strArray1[index].IndexOf("]");
          key1 = strArray1[index].Substring(startIndex, num - startIndex);
          if (!INISetting.groups.ContainsKey(key1))
            INISetting.groups.Add(key1, new INISetting.INI_Values());
        }
        else
        {
          string[] strArray2 = strArray1[index].Split(new char[1]
          {
            '='
          }, StringSplitOptions.RemoveEmptyEntries);
          string key2 = strArray2[0].Trim().Replace("&equal;", "=");
          INISetting.ValueResult valueResult = strArray2.Length != 2 ? new INISetting.ValueResult("") : new INISetting.ValueResult(strArray2[1].Trim().Replace("&equal;", "="));
          if (!INISetting.groups.ContainsKey(key1))
            INISetting.groups.Add(key1, new INISetting.INI_Values());
          INISetting.groups[key1][key2] = valueResult;
        }
      }
    }
  }

  public static T GetValue<T>(string group, string key)
  {
    T val;
    INISetting.TryGetGroupValue<T>(group, key, out val);
    return val;
  }

  public static T GetValue<T>(string key)
  {
    T val;
    INISetting.TryGetGroupValue<T>(INISetting.defaultGroup, key, out val);
    return val;
  }

  public static bool TryGetGroupValue<T>(string group, string key, out T val)
  {
    if (INISetting.groups.ContainsKey(group))
    {
      if (INISetting.groups[group].ContainsKey(key))
      {
        val = INISetting.groups[group][key].GetValue<T>();
        return true;
      }
      string contents = "";
      foreach (KeyValuePair<string, INISetting.INI_Values> group1 in INISetting.groups)
      {
        contents += string.Format("[{0}]\r\n", (object) group1.Key);
        foreach (KeyValuePair<string, INISetting.ValueResult> keyValuePair in (Dictionary<string, INISetting.ValueResult>) group1.Value)
          contents += string.Format("{0}={1}\r\n", (object) keyValuePair.Key, (object) keyValuePair.Value.ToString());
        if (group1.Key == group)
          contents += string.Format("#{0}={1}\r\n", (object) key, (object) default (T));
      }
      File.WriteAllText("settings.ini", contents, Encoding.UTF8);
    }
    else
    {
      string str = "";
      foreach (KeyValuePair<string, INISetting.INI_Values> group1 in INISetting.groups)
      {
        str += string.Format("[{0}]\r\n", (object) group1.Key);
        foreach (KeyValuePair<string, INISetting.ValueResult> keyValuePair in (Dictionary<string, INISetting.ValueResult>) group1.Value)
          str += string.Format("{0}={1}\r\n", (object) keyValuePair.Key, (object) keyValuePair.Value.ToString());
      }
      File.WriteAllText("settings.ini", str + string.Format("#[{0}]\r\n", (object) group) + string.Format("#{0}={1}\r\n", (object) key, (object) default (T)), Encoding.UTF8);
    }
    val = default (T);
    return false;
  }

  public static T GetGroupValueWithAdd<T>(string group, string key, T defaultValue)
  {
    if (!INISetting.groups.ContainsKey(group))
    {
      string str = "";
      foreach (KeyValuePair<string, INISetting.INI_Values> group1 in INISetting.groups)
      {
        str += string.Format("[{0}]\r\n", (object) group1.Key);
        foreach (KeyValuePair<string, INISetting.ValueResult> keyValuePair in (Dictionary<string, INISetting.ValueResult>) group1.Value)
          str += string.Format("{0}={1}\r\n", (object) keyValuePair.Key, (object) keyValuePair.Value.ToString());
      }
      File.WriteAllText("settings.ini", str + string.Format("[{0}]\r\n", (object) group) + string.Format("{0}={1}\r\n", (object) key, (object) defaultValue), Encoding.UTF8);
      T obj = defaultValue;
      INISetting.INI_Values iniValues = new INISetting.INI_Values();
      iniValues.Add(key, new INISetting.ValueResult(defaultValue.ToString()));
      INISetting.groups.Add(group, iniValues);
      return obj;
    }
    INISetting.INI_Values group2 = INISetting.groups[group];
    T obj1;
    if (!group2.ContainsKey(key) || group2[key].IsEmpty)
    {
      obj1 = defaultValue;
      string contents = "";
      bool flag = group2.ContainsKey(key);
      if (flag && group2[key].IsEmpty)
        group2[key] = new INISetting.ValueResult(defaultValue.ToString());
      else if (!flag)
        group2.Add(key, new INISetting.ValueResult(defaultValue.ToString()));
      foreach (KeyValuePair<string, INISetting.INI_Values> group1 in INISetting.groups)
      {
        contents += string.Format("[{0}]\r\n", (object) group1.Key);
        foreach (KeyValuePair<string, INISetting.ValueResult> keyValuePair in (Dictionary<string, INISetting.ValueResult>) group1.Value)
          contents += string.Format("{0}={1}\r\n", (object) keyValuePair.Key, (object) keyValuePair.Value.ToString());
      }
      File.WriteAllText("settings.ini", contents, Encoding.UTF8);
    }
    else
      obj1 = group2[key].GetValue<T>();
    return obj1;
  }

  public static bool TryGetValue<T>(string key, out T val) => INISetting.TryGetGroupValue<T>(INISetting.defaultGroup, key, out val);

  public static T GetValueWithAdd<T>(string key, T defaultValue) => INISetting.GetGroupValueWithAdd<T>(INISetting.defaultGroup, key, defaultValue);

  public static bool Exists(string group, string key) => INISetting.groups.ContainsKey(group) && INISetting.groups[group].ContainsKey(key);

  public static void SetValue(string group, string key, object value)
  {
    if (INISetting.Exists(group, key))
    {
      INISetting.groups[group][key] = new INISetting.ValueResult(value.ToString());
    }
    else
    {
      if (!INISetting.groups.ContainsKey(group))
        INISetting.groups.Add(group, new INISetting.INI_Values());
      if (INISetting.groups[group].ContainsKey(key))
        return;
      INISetting.groups[group].Add(key, new INISetting.ValueResult(value.ToString()));
    }
  }

  public static void SetValue(string key, string value) => INISetting.SetValue(INISetting.defaultGroup, key, (object) value);

  public static void Save()
  {
    string contents = "";
    foreach (KeyValuePair<string, INISetting.INI_Values> group in INISetting.groups)
    {
      contents += string.Format("[{0}]\r\n", (object) group.Key);
      foreach (KeyValuePair<string, INISetting.ValueResult> keyValuePair in (Dictionary<string, INISetting.ValueResult>) group.Value)
        contents += string.Format("{0}={1}\r\n", (object) keyValuePair.Key, (object) keyValuePair.Value.ToString());
    }
    File.WriteAllText("settings.ini", contents, Encoding.UTF8);
  }

  protected class INI_Values : Dictionary<string, INISetting.ValueResult>
  {
  }

  public class ValueResult
  {
    private string value;

    public bool IsEmpty => string.IsNullOrEmpty(this.value);

    public ValueResult(string _value) => this.value = _value;

    public T GetValue<T>()
    {
      if (string.IsNullOrEmpty(this.value))
        return default (T);
      if (typeof (T).IsEnum)
        return (T) Enum.Parse(typeof (T), this.value);
      return typeof (T) == typeof (float) ? (T) Convert.ChangeType((object) this.value.Replace(",", "."), typeof (T)) : (T) Convert.ChangeType((object) this.value, typeof (T));
    }

    public override string ToString() => this.value;
  }
}
