using UnityEngine;
using Zenject;

public class WireInstaller : MonoInstaller<WireInstaller>
{
    public Transform target;
    public RectTransform wiresRoot;
    public Transform dragPlane;

    public override void InstallBindings()
    {
        Container.Bind(typeof(IWireFactory), typeof(ILineWireRenderer)).To<LineRendererWireFactory>().AsSingle();
        Container.Bind<IWireRenderer>().To<WireRenderer>().AsSingle();
        Container.Bind<IWire>().To<Wire>().AsSingle();
        Container.Bind<IInputManager>().To<WiresInputManager>().AsSingle();
        Container.BindInstance(target.position);
        if (wiresRoot != null) Container.BindInstance(wiresRoot);
        if (dragPlane != null) Container.BindInstance(dragPlane);
        var mainCam = Camera.main;
        if (mainCam != null) Container.BindInstance(mainCam);
        Container.BindInstance(0.5f);
    }
}
