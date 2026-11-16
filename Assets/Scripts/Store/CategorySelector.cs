using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class CategorySelector : MonoBehaviour
{
    [Header("Settings for Upgrade Panels")]
    [SerializeField] private CanvasGroup gunCG, characterSkinCG, itemCG;
    [SerializeField] Button gunCGButton, characterSkinCGButton, itemCGButton;
    [SerializeField] private GameObject [] onSelectImage;
    
    [Header("General Settings")]
    [SerializeField] private float fadeDuration = 0.55f, fadeOutDuration = 0.05f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gunCGButton.onClick.AddListener(FadeInGunCG);
        characterSkinCGButton.onClick.AddListener(FadeInCharacterSkinCG);
        itemCGButton.onClick.AddListener(FadeInItemCG);

        FadeInGunCG();
    }

    #region CanvasGroup Fade Control Methods
    private void SetCanvasGroupActive(CanvasGroup tUI)
    {
        tUI.DOKill(); 
        tUI.gameObject.SetActive(true);
        tUI.alpha = 0; 
        tUI.DOFade(1, fadeDuration).SetUpdate(true); 
        tUI.interactable = true;
        tUI.blocksRaycasts = true;
    }

    private void SetCanvasGroupInActive(CanvasGroup tUI)
    {
        tUI.DOKill(); 
        tUI.interactable = false;
        tUI.blocksRaycasts = false;
        tUI.DOFade(0, fadeOutDuration)
            .SetUpdate(true) 
            .OnComplete(() => { tUI.gameObject.SetActive(false); });
    }

    private void FadeInCanvasGroupActive(CanvasGroup tUI)
    {
        tUI.DOKill();
        tUI.alpha = 0; 
        tUI.DOFade(1, fadeDuration).SetUpdate(true); 
    }

    private void FadeOutCanvasGroupInActive(CanvasGroup tUI)
    {
        tUI.DOKill();
        tUI.DOFade(0.5f, fadeDuration).SetUpdate(true);
    }
    #endregion

    #region Main Panels
    void FadeInGunCG()
    {        
        foreach(var obj in onSelectImage)
        {
            obj.SetActive(false);
        }

        onSelectImage[0].SetActive(true);
        SetCanvasGroupActive(gunCG);
        SetCanvasGroupInActive(itemCG);
        SetCanvasGroupInActive(characterSkinCG);
    }

    void FadeInCharacterSkinCG()
    {
        foreach(var obj in onSelectImage)
        {
            obj.SetActive(false);
        }

        onSelectImage[1].SetActive(true);
        SetCanvasGroupActive(characterSkinCG);
        SetCanvasGroupInActive(gunCG);
        SetCanvasGroupInActive(itemCG);
    }

    void FadeInItemCG()
    {        
        foreach(var obj in onSelectImage)
        {
            obj.SetActive(false);
        }

        onSelectImage[2].SetActive(true);
        SetCanvasGroupActive(itemCG);
        SetCanvasGroupInActive(gunCG);
        SetCanvasGroupInActive(characterSkinCG);
    }
    #endregion
}