using UnityEngine;
using System.Collections;

public class rlib {

    public static void Log(string template, object arg0)
    {
        Debug.Log(string.Format(template, arg0.ToString()));
    }

    public static void Log(string template, object arg0, object arg1)
    {
        Debug.Log(string.Format(template, arg0.ToString(), arg1.ToString()));
    }

    public static void Log(string template, object arg0, object arg1, object arg2)
    {
        Debug.Log(string.Format(template, arg0.ToString(), arg1.ToString(), arg2.ToString()));
    }
}
