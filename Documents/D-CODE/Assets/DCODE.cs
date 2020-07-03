using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using KModkit;
using System.Text.RegularExpressions;
using System.Linq;

public class DCODE : MonoBehaviour {
 public KMBombModule module;
public KMSelectable[] digits;
public TextMesh[] texts;
//public Color[] fontColours;
public KMBombInfo bomb;
public KMAudio sound;
string[] glyphpos = new string[] { "left", "middle", "right" };
int[] values = new int[3];
int stageCounter = 1;
bool interaction = false;
static int moduleidcounter = 1;
int moduleid;
bool moduleSolved;
    void Awake()
    {
        moduleid = moduleidcounter++;
        foreach (KMSelectable digit in digits) { 
            KMSelectable pressedDigit = digit;
            digit.OnInteract += delegate ()
            {
                pressDigit(pressedDigit); return false;
            };
        }
        module.OnActivate += OnActivate;
    }
    // Use this for initialization
    void Start () {
        for (int i = 0; i < 3; i++)
        {
            values[i] = UnityEngine.Random.Range(0, 10);
        }
        Debug.LogFormat("[D-CODE #{0}] The glyphs from left to right must be pressed at {1}, {2}, and {3}.", moduleid, values[0], values[1], values[2]);
	}

    void OnActivate()
    {
        for (int i = 0; i < 3; i++)
        {
            texts[i].text = values[i].ToString();
        }
        interaction = true;
    }
	
	void pressDigit (KMSelectable digit) {
        if (!moduleSolved && interaction) {
            digit.AddInteractionPunch(.5f);
            Debug.LogFormat("[D-CODE #{0}] You pressed the {1} glyph at {2}.", moduleid, glyphpos[Array.IndexOf(digits, digit)], bomb.GetFormattedTime());
            if (Math.Floor(bomb.GetTime() % 60 % 10) != int.Parse(digit.GetComponentInChildren<TextMesh>().text))
            {
                Debug.LogFormat("[D-CODE #{0}] That was incorrect. Strike.", moduleid);
                module.HandleStrike();
            }
            else
            {
                Debug.LogFormat("[D-CODE #{0}] That was correct.", moduleid);
                digit.Highlight.gameObject.SetActive(false);
                digit.GetComponentInChildren<TextMesh>().text = "";
                switch (stageCounter)
                {
                    case 3:
                        sound.PlaySoundAtTransform("SolveSound", transform);
                        Debug.LogFormat("[D-CODE #{0}] Module solved.", moduleid);
                        module.HandlePass();
                        moduleSolved = true;
                        break;
                    default:
                        sound.PlaySoundAtTransform("StageSound", transform);
                        stageCounter++;
                        break;

                }
            }     
        }
	}

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <glyph> at <#> [Presses the specified glyph when the last digit of the bomb's timer is '#'] | Valid glyphs are left, middle, and right";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 4)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 4)
            {
                if (glyphpos.Contains(parameters[1].ToLower()) && parameters[2].ToLower().Equals("at"))
                {
                    int digit = 0;
                    if (int.TryParse(parameters[3], out digit))
                    {
                        if (digit < 0 || digit > 9)
                        {
                            yield return "sendtochaterror The specified time to press the " + parameters[1] + " glyph '" + parameters[3] + "' is out of range 0-9!";
                            yield break;
                        }
                        if (digits[Array.IndexOf(glyphpos, parameters[1].ToLower())].GetComponentInChildren<TextMesh>().text.Equals(""))
                        {
                            yield return "sendtochaterror The " + parameters[1] + " glyph has already been pressed!";
                            yield break;
                        }
                        while ((int)bomb.GetTime() % 10 != digit) { yield return "trycancel Halted waiting to press due to a request to cancel!"; }
                        digits[Array.IndexOf(glyphpos, parameters[1].ToLower())].OnInteract();
                    }
                    else
                    {
                        yield return "sendtochaterror The specified time to press the " + parameters[1] + " glyph '" + parameters[3] + "' is invalid!";
                    }
                }
                else if (glyphpos.Contains(parameters[1].ToLower()) && !parameters[2].ToLower().Equals("at"))
                {
                    yield return "sendtochaterror Invalid command format! Expected 'at' but received '" + parameters[2] + "'!";
                }
                else
                {
                    yield return "sendtochaterror The specified glyph '" + parameters[1] + "' is invalid!";
                }
            }
            else if (parameters.Length == 3)
            {
                if (glyphpos.Contains(parameters[1].ToLower()) && parameters[2].ToLower().Equals("at"))
                {
                    yield return "sendtochaterror Please specify when to press the " + parameters[1] + " glyph!";
                }
                else if (glyphpos.Contains(parameters[1].ToLower()) && !parameters[2].ToLower().Equals("at"))
                {
                    yield return "sendtochaterror Invalid command format! Expected 'at' but received '" + parameters[2] + "'!";
                }
                else
                {
                    yield return "sendtochaterror The specified glyph '" + parameters[1] + "' is invalid!";
                }
            }
            else if (parameters.Length == 2)
            {
                if (glyphpos.Contains(parameters[1].ToLower()))
                {
                    yield return "sendtochaterror Please specify when to press the " + parameters[1] + " glyph!";
                }
                else
                {
                    yield return "sendtochaterror The specified glyph '" + parameters[1] + "' is invalid!";
                }
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify which glyph to press and when to press it!";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!interaction) { yield return true; yield return new WaitForSeconds(0.1f); }
        while (!moduleSolved)
        {
            if (texts[0].text != "")
            {
                if ((int)bomb.GetTime() % 10 == int.Parse(texts[0].text))
                {
                    digits[0].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            if (texts[1].text != "")
            {
                if ((int)bomb.GetTime() % 10 == int.Parse(texts[1].text))
                {
                    digits[1].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            if (texts[2].text != "")
            {
                if ((int)bomb.GetTime() % 10 == int.Parse(texts[2].text))
                {
                    digits[2].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield return true;
        }
    }
}