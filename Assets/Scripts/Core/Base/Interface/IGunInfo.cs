using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace OpenGS
{
    public interface IGunInfo
    {
        string Name();
        int MagazineCount();
        int MagazineMaxCount();
        Sprite GunBigIcon();
        Sprite GunSilhouette();
    }

    public interface IReloadable
    {
        bool CanReload();
        void ReloadStart();
        void ReloadCancel();
    }

    public interface IShotable
    {
        bool CanShot();
        void Shot();
    }

}