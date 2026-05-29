# -*- coding: utf-8 -*-
import os

with open('MainWindow.xaml.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Greeting
content = content.replace(
    'HomeGreetingLabel.Text = !string.IsNullOrEmpty(_session.Username)\n                ? $"{greeting}, {_session.Username} ðŸ‘‹\\nðŸ“¢ {currentNews}"\n                : "Listo para jugar";',
    'HomeGreetingLabel.Text = !string.IsNullOrEmpty(_session.Username)\n                ? $"{greeting}, {_session.Username}\\n>> {currentNews}"\n                : "Listo para jugar";'
)

# Greeting fallback (sometimes the string could have changed)
content = content.replace('ðŸ‘‹', '')
content = content.replace('ðŸ“¢', '>> ')

# AgregarLog
content = content.replace(
    'AgregarLog($"ðŸ›¡ï¸  Sistema Operativo Kraken v{liveVersion} â€” Núcleo estable.");',
    'AgregarLog($"[i] Sistema Operativo Kraken v{liveVersion} - Núcleo estable.");'
)
content = content.replace('ðŸ›¡ï¸ ', '[i]')
content = content.replace('â€”', '-')

with open('MainWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(content)

# HubView
with open('Modules/HubView.xaml', 'r', encoding='utf-8') as f:
    hub = f.read()

hub = hub.replace('Background="#EE081218"', 'Background="#1C1C1E"')
hub = hub.replace('BorderBrush="#2B4A55"', 'BorderBrush="#38383A"')

with open('Modules/HubView.xaml', 'w', encoding='utf-8') as f:
    f.write(hub)

print('Fixed strings and HubView')
