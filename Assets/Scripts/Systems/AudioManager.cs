using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioMixer m_Mixer;
    [SerializeField]
    private Slider music_Slider;
    [SerializeField]
    private Slider sfx_Slider;

    void Start()
    {
        music_Slider.minValue = 0;
        music_Slider.maxValue = 10;
        music_Slider.wholeNumbers = true;

        sfx_Slider.minValue = 0;
        sfx_Slider.maxValue = 10;
        sfx_Slider.wholeNumbers = true;

        music_Slider.value = PlayerPrefs.GetFloat("MusicVolume", 8f);
        sfx_Slider.value = PlayerPrefs.GetFloat("SFXVolume", 8f);

        SetMusicVolume(music_Slider.value);
        SetSFXVolume(sfx_Slider.value);
    }



    public void SetMusicVolume(float volume)
    {
        volume *= 10;
        if (volume == 0)
        {
            m_Mixer.SetFloat("MusicVolume", -80);
        }
        else
        {
            m_Mixer.SetFloat("MusicVolume", Mathf.Log10(volume / 100f) * 20); ;
        }
        PlayerPrefs.SetFloat("MusicVolume", music_Slider.value);

    }


    public void SetSFXVolume(float volume)
    {
        volume *= 10;
        if (volume == 0)
        {
            m_Mixer.SetFloat("SFXVolume", -80);
        }
        else
        {
            m_Mixer.SetFloat("SFXVolume", Mathf.Log10(volume / 100f) * 20);
        }
        PlayerPrefs.SetFloat("SFXVolume", sfx_Slider.value);
    }
}
