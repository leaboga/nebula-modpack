# -*- coding: utf-8 -*-
import os

def fix_cs(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Greeting
    content = content.replace(
        'HomeGreetingLabel.Text = !string.IsNullOrEmpty(_session.Username)\n                ? $"{greeting}, {_session.Username} ðŸ‘‹\\nðŸ“¢ {currentNews}"\n                : "Listo para jugar";',
        'HomeGreetingLabel.Text = !string.IsNullOrEmpty(_session.Username)\n                ? $"{greeting}, {_session.Username}\\n>> {currentNews}"\n                : "Listo para jugar";'
    )
    
    # AgregarLog
    content = content.replace(
        'AgregarLog($"ðŸ›¡ï¸  Sistema Operativo Kraken v{liveVersion} â€” Núcleo estable.");',
        'AgregarLog($"[i] Sistema Operativo Kraken v{liveVersion} - Núcleo estable.");'
    )

    # Broken borders
    content = content.replace('â• ', '=')

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

fix_cs('MainWindow.xaml.cs')

# Fix HubView
with open('Modules/HubView.xaml', 'r', encoding='utf-8') as f:
    hub = f.read()

hub = hub.replace('Background="#EE081218"', 'Background="#1C1C1E"')
hub = hub.replace('BorderBrush="#2B4A55"', 'BorderBrush="#38383A"')

with open('Modules/HubView.xaml', 'w', encoding='utf-8') as f:
    f.write(hub)

print('Strings and HubView fixed.')
