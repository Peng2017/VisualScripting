using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.CSharp;
using UnityEngine;

public class VSGraphAsset : MonoBehaviour
{
    public int Count = 0;
    public void Update()
    {
        Count = (Count + 1);
        Debug.Log(Count);
    }
}