using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class IntroOnMenuStart : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VideoOverlayPlayer overlay;
    [SerializeField] private VideoClip introClip;
    [SerializeField] private GameObject menuRoot;

    // Isto garante: só toca 1x por arranque do jogo (mesma sessão)
    private static bool s_introPlayedThisLaunch = false;

    void Start()
    {
        // Se faltar o menuRoot, não há nada a fazer
        if (!menuRoot) return;

        // Por segurança, garante que o menu está visível por default
        menuRoot.SetActive(true);

        // Se já tocou nesta sessão, não toca outra vez (ex.: voltou do Tutorial)
        if (s_introPlayedThisLaunch) return;

        // Se faltarem refs do vídeo, não bloqueia o menu
        if (!overlay || !introClip)
        {
            s_introPlayedThisLaunch = true; // marca como "já tratado" para não tentar repetir
            return;
        }

        // Marca já para evitar tocar duas vezes por race conditions / reloads
        s_introPlayedThisLaunch = true;

        // Esconde menu e toca intro
        menuRoot.SetActive(false);

        // (Opcional, mas útil) garante que o overlay também desliga o menu enquanto toca
        overlay.defaultClip = introClip;
        overlay.disableWhilePlaying = new GameObject[] { menuRoot };

        overlay.Play(introClip, () =>
        {
            if (menuRoot) menuRoot.SetActive(true);
        });
    }
}
