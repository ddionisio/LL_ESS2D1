using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ModalVictory : M8.ModalController, M8.IModalPush {
    public const string parmPopulation = "pop";
    public const string parmPopulationMax = "popMax";

    public const string parmHouse = "house";
    public const string parmHouseMax = "houseMax";

    [Header("Display")]
    public TMP_Text populationLabel;
    public MedalWidget populationMedal;

    public TMP_Text houseCountLabel;
    public MedalWidget houseMedal;

    [Header("SFX")]
    [M8.SoundPlaylist]
    public string sfxVictory;

    void M8.IModalPush.Push(M8.GenericParams parms) {
        int pop = 0, popMax = 0, house = 0, houseMax = 0;

        if(parms != null) {
            if(parms.ContainsKey(parmPopulation))
                pop = parms.GetValue<int>(parmPopulation);
            if(parms.ContainsKey(parmPopulationMax))
                popMax = parms.GetValue<int>(parmPopulationMax);
            if(parms.ContainsKey(parmHouse))
                house = parms.GetValue<int>(parmHouse);
            if(parms.ContainsKey(parmHouseMax))
                houseMax = parms.GetValue<int>(parmHouseMax);
        }

        if(populationLabel) populationLabel.text = pop.ToString(); //string.Format("{0}/{1}", pop, popMax);
        if(popMax > 0 && populationMedal) populationMedal.ApplyRank((float)pop / popMax);

        if(houseCountLabel) houseCountLabel.text = house.ToString(); //string.Format("{0}/{1}", house, houseMax);
        if(houseMax > 0 && houseMedal) houseMedal.ApplyRank((float)house / houseMax);

        if(!string.IsNullOrEmpty(sfxVictory))
            M8.SoundPlaylist.instance.Play(sfxVictory, false);
    }
}
