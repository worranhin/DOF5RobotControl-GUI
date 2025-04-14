from typing import Dict
from urllib.parse import urljoin
import requests

BASE_URL = "http://localhost:5162/"

def get_base_url():
    """
    Get current base url
    """
    return BASE_URL


def set_base_url(url: str):
    """
    Set the base url of the server.
    """
    global BASE_URL
    BASE_URL = url


def get_current_state(urlbase="http://localhost:5162/"):
    """
    Get the current state of the robot

    Args:
        urlbase: The base url of the server

    Returns:
        A tuple containing the current state and the HTTP status code.
    """
    get_url = urljoin(urlbase, "/Robot/current")
    response = requests.get(get_url)

    if response.status_code == 200:
        data = response.json()
        return data, response.status_code
    else:
        return None, response.status_code


def get_target_state(urlbase="http://localhost:5162/"):
    """
    Get the target state of the robot

    Args:
        urlbase: The base url of the server

    Returns:
        A tuple containing the target state and the HTTP status code.
    """
    get_url = urljoin(urlbase, "/Robot/target")
    response = requests.get(get_url)

    if response.status_code == 200:
        data = response.json()
        return data, response.status_code
    else:
        return None, response.status_code


def put_target_joint(joint_values: Dict[str, float], urlbase="http://localhost:5162/"):
    """
    update the target by joint values

    Args:
        joint_values: A dictionary contains joint values, e.g., {"r1": 1.0, "p2": 2.0, "p3": 3.0, "p4": 4.0, "r5": 5.0}.
        urlbase: The base url of the server

    Returns:
        A tuple containing the response data (if any) and the HTTP status code.
    """
    put_url = urljoin(urlbase, "/Robot/joint") 
    headers = {"Content-Type": "application/json"}
    response = requests.put(put_url, json=joint_values, headers=headers)

    if response.status_code == 204:
        return None, response.status_code
    else:
        return None, response.status_code