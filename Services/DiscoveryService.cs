using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace KrakenLauncher.Services
{
    public class TutorialStep
    {
        public string TargetElement { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public Placement TargetPlacement { get; set; } = Placement.Bottom;
    }

    public enum Placement { Top, Bottom, Left, Right }

    public class DiscoveryService
    {
        private static DiscoveryService? _instance;
        public static DiscoveryService Instance => _instance ??= new DiscoveryService();

        private List<TutorialStep> _steps = new();
        private int _currentStep = -1;
        private Grid? _rootContainer;
        private Window? _owner;

        public void Initialize(Window owner, Grid rootContainer)
        {
            _owner = owner;
            _rootContainer = rootContainer;
            DefineSteps();
        }

        private void DefineSteps()
        {
            _steps = new List<TutorialStep>
            {
                new TutorialStep { 
                    TargetElement = "SideBar", 
                    Title = "Navegación Consolidada", 
                    Content = "Hemos agrupado todo en 4 centros principales: Centro de Mando, Sistemas Pepa, Biblioteca Mod y Red Abisal. Más limpio y ordenado." 
                },
                new TutorialStep { 
                    TargetElement = "PlayButton", 
                    Title = "Inicia la Aventura", 
                    Content = "El motor Kraken está listo. Pulsa Jugar para entrar al mundo con todas las optimizaciones aplicadas." 
                },
                new TutorialStep { 
                    TargetElement = "UpdateBadge", 
                    Title = "Núcleo Siempre Al Día", 
                    Content = "Aquí verás si hay nuevas versiones del motor Kraken. Se actualiza solo, pero puedes forzarlo aquí." 
                },
                new TutorialStep { 
                    TargetElement = "SkinPanel", 
                    Title = "Tu Identidad", 
                    Content = "Haz clic en tu avatar o nombre para cambiar tu skin y gestionar tus cuentas." 
                }
            };
        }

        public void Start(UserSession session)
        {
            if (session.HasFinishedDiscovery) return;
            _currentStep = 0;
            ShowStep(_currentStep);
        }

        public void NextStep()
        {
            _currentStep++;
            if (_currentStep >= _steps.Count)
            {
                EndTutorial();
                return;
            }
            ShowStep(_currentStep);
        }

        private void ShowStep(int index)
        {
            var step = _steps[index];
            // Here you would find the element in the UI and show a popup
            // For now, I'll provide the logic to be called from MainWindow
        }

        private void EndTutorial()
        {
            // Mark as finished
        }
    }
}
