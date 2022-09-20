using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggledText : MonoBehaviour
{
    public List<string> states = new List<string>()
    {
        "1",
        "2"
    };

    private int index = 0;
    private Text _text;

    private void Start()
    {
        _text = GetComponent<Text>();
    }

    public void Toggled(int state)
    {
        index = state;
        
        if (index >= states.Count)
            index = 0;

        _text.text = states[index];
    }

    public void Toggled(bool state)
    {
        var target = state ? 1 : 0;
        Toggled(target);
    }
    
    public void Toggled()
    {
        Toggled(index++);
    }
}
