#!/bin/bash

if [ $BASH != '/bin/bash' ]; then
    echo 'Что-то пошло не так - у вас тут не bash.'
    exit
fi

if [ `lsb_release -s -i` == 'Ubuntu' ]; then

    sudo apt-get update && apt-get upgrade -y && apt-get dist-upgrade -y

    sudo apt-get -y install make openssl ca-certificates curl software-properties-common gnupg mc htop git net-tools

    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

    sudo apt-get update
    sudo apt-get -y install docker-ce docker-ce-cli containerd.io

    sudo curl -L "https://github.com/docker/compose/releases/download/1.29.2/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose

fi

echo 'Done.'
